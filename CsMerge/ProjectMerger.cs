﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

using Cpc.CsMerge.Core;
using Cpc.CsMerge.Core.Parsing;

using NLog;

namespace CsMerge {

  /// <summary>
  /// Merges project files
  /// </summary>
  public class ProjectMerger {

    private static Dictionary<string, TItem> GetItemIndex<TItem>( ProjectFile localProj, Predicate<Item> include ) where TItem: Item {
      return GetItems<TItem>( localProj, include ).ToDictionary( r => r.Key );
    }

    private static IEnumerable<TItem> GetItems<TItem>( ProjectFile localProj, Predicate<Item> include = null ) where TItem: Item {
      include = include ?? ( p => true );

      return localProj.ItemGroups.SelectMany( ig => ig.Items.Where( i => i is TItem && include( i ) ).Cast<TItem>() );
    }

    private static IEnumerable<Reference> MergeReferences(
      PackagesInfo packageInfo,
      IReadOnlyCollection<Reference> baseRefs,
      IReadOnlyCollection<Reference> localRefs,
      IReadOnlyCollection<Reference> theirRefs, ConflictResolver<Reference> resolver ) {

      return MergePackageReferences( packageInfo,
        baseRefs.Where( packageInfo.IsPackageReference ),
        localRefs.Where( packageInfo.IsPackageReference ),
        theirRefs.Where( packageInfo.IsPackageReference ) )
          .Concat( MergeNonPackageReferencesbaseRefs(
          baseRefs.Where( r => !packageInfo.IsPackageReference( r ) ),
          localRefs.Where( r => !packageInfo.IsPackageReference( r ) ),
          theirRefs.Where( r => !packageInfo.IsPackageReference( r ) ), resolver ) );
    }

    private static IEnumerable<Reference> MergeNonPackageReferencesbaseRefs(
      IEnumerable<Reference> baseRefs,
      IEnumerable<Reference> localRefs,
      IEnumerable<Reference> theirRefs,
      ConflictResolver<Reference> resolver ) {

      var logger = LogManager.GetCurrentClassLogger();

      var baseByName = baseRefs.ToDictionary( r => r.ReferenceAssemblyName.Name, r => r );
      var localByName = localRefs.ToDictionary( r => r.ReferenceAssemblyName.Name, r => r );
      var theirByName = theirRefs.ToDictionary( r => r.ReferenceAssemblyName.Name, r => r );

      foreach ( var name in baseByName.Keys.Concat( localByName.Keys ).Concat( theirByName.Keys ).Distinct() ) {
        Reference b = baseByName.ContainsKey( name ) ? baseByName[name] : null;
        Reference m = localByName.ContainsKey( name ) ? localByName[name] : null;
        Reference t = theirByName.ContainsKey( name ) ? theirByName[name] : null;

        var mergeResult = MergeHelper<Reference>.Merge( b, m, t, resolver );

        if ( mergeResult.ResolvedItem != null ) {
          logger.Info( "Resolved " + mergeResult.ResolvedItem + " after " + mergeResult.MergeType );
          yield return mergeResult.ResolvedItem;
        }
        else {
          logger.Info( "Removed " + b + " because of " + mergeResult.MergeType );
        }
      }
    }

    private static IEnumerable<Reference> MergePackageReferences(
      PackagesInfo packageInfo,
      IEnumerable<Reference> baseRefs,
      IEnumerable<Reference> localRefs,
      IEnumerable<Reference> theirRefs ) {

      // Discard packages that are no longer installed 
      baseRefs = baseRefs.Where( packageInfo.IsPackageInstalled );
      localRefs = localRefs.Where( packageInfo.IsPackageInstalled );
      theirRefs = theirRefs.Where( packageInfo.IsPackageInstalled );

      return baseRefs.Union( localRefs ).Union( theirRefs );
    }

    public static IEnumerable<Item> Merge( string name,
      PackagesInfo info,
      XDocument baseDocument,
      XDocument localDocument,
      XDocument theirDocument,
      ConflictResolver<Reference> referenceResolver,
      ConflictResolver<Item> itemResolver ) {

      CsProjParser parser = new CsProjParser();
      var localProj = parser.Parse( name, localDocument );
      var theirProj = parser.Parse( name, theirDocument );
      var baseProj = parser.Parse( name, baseDocument );

      var localItems = GetItemIndex<Item>( localProj, i => !( i is Reference ) );
      var theirItems = GetItemIndex<Item>( theirProj, i => !( i is Reference ) );
      var baseItems = GetItemIndex<Item>( baseProj, i => !( i is Reference ) );

      var localRefs = GetItems<Reference>( localProj ).ToArray();
      var theirRefs = GetItems<Reference>( theirProj ).ToArray();
      var baseRefs = GetItems<Reference>( baseProj ).ToArray();

      return MergeNonReference( localItems, theirItems, baseItems, itemResolver ).Concat(
        MergeReferences( info, baseRefs, localRefs, theirRefs, referenceResolver ) );
    }

    private static IEnumerable<Item> MergeNonReference(
      Dictionary<string, Item> localItems,
      Dictionary<string, Item> theirItems,
      Dictionary<string, Item> baseItems,
     ConflictResolver<Item> resolver ) {

      var logger = LogManager.GetCurrentClassLogger();

      foreach (
        string key in
          localItems.Keys
            .Union( theirItems.Keys )
            .Union( baseItems.Keys ) ) {
        Item b = baseItems.ContainsKey( key ) ? baseItems[key] : null;
        Item m = localItems.ContainsKey( key ) ? localItems[key] : null;
        Item t = theirItems.ContainsKey( key ) ? theirItems[key] : null;

        var mergeResult = MergeHelper<Item>.Merge( b, m, t, resolver );

        if ( mergeResult.ResolvedItem != null ) {
          logger.Info( "Resolved " + mergeResult.ResolvedItem + " after " + mergeResult.MergeType );
          yield return mergeResult.ResolvedItem;
        }
        else {
          logger.Info( "Removed " + b + " because of " + mergeResult.MergeType );
        }
      }
    }
  }
}