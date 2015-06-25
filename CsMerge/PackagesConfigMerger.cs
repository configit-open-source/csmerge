using System;
using System.Collections.Generic;
using System.Linq;

using Cpc.CsMerge.Core;

namespace PackagesMerge {
  public class PackagesConfigMerger {
    private static IDictionary<string, Package> GetIndex( IEnumerable<Package> pc ) {
      return pc.ToDictionary( p => p.Id, p => p );
    }

    public static IEnumerable<Package> Merge(
      IEnumerable<Package> @base,
      IEnumerable<Package> mine,
      IEnumerable<Package> theirs,
      Func<Conflict<Package>, Package> conflictResolver ) {
      var logger = NLog.LogManager.GetCurrentClassLogger();
      var baseIds = GetIndex( @base );
      var myIds = GetIndex( mine );
      var theirIds = GetIndex( theirs );

      List<Package> packages = new List<Package>();

      foreach (
        string id in
          baseIds.Keys.Union( myIds.Keys ).Union( theirIds.Keys ).OrderBy( i => i, StringComparer.OrdinalIgnoreCase ) ) {
        var b = baseIds.ContainsKey( id ) ? baseIds[id] : null;
        var m = myIds.ContainsKey( id ) ? myIds[id] : null;
        var t = theirIds.ContainsKey( id ) ? theirIds[id] : null;

        if ( m == null && t == null ) {
          logger.Info( b + " deleted in both branches" );
          continue; // deleted in both 
        }

        if ( m == t ) {
          logger.Info( m + " matches between branches" );
          packages.Add( m ); // identical
          continue;
        }

        if ( b != null && m == null && !t.AllowedVersions.Equals( b.AllowedVersions ) ) {
          // Mine deleted something modified in theirs
          logger.Info( "Local deleted " + b + " while remote changed to " + t );
          var resolved = conflictResolver( new Conflict<Package>( b, m, t ) );
          if ( resolved != null ) {
            packages.Add( resolved );
          }
          continue;
        }

        if ( b != null && t == null && !m.AllowedVersions.Equals( b.AllowedVersions ) ) {
          // Theirs deleted something modified in mine
          logger.Info( "Patch deleted " + b + " while local changed to " + m );
          var resolved = conflictResolver( new Conflict<Package>( b, m, t ) );
          if ( resolved != null ) {
            packages.Add( resolved );
          }
          continue;
        }

        if ( t == null ) {
          packages.Add( m );
          logger.Info( "Local added " + m );
        }
        else if ( m == null ) {
          logger.Info( "Local added " + t );
          packages.Add( t );
        }
        else {
          var mineNotComparingOnVersion = new Package( m.Id, t.Version, t.TargetFramework, t.AllowedVersions );

          if ( mineNotComparingOnVersion == m ) {
            var mineHigher = m.Version.CompareTo( t.Version ) >= 0;
            logger.Info( "Both modified, picking " + ( mineHigher ? m : t ) + " over " + ( mineHigher ? t : m ) );
            packages.Add( mineHigher ? m : t );
          }
          else {
            var resolved = conflictResolver( new Conflict<Package>( b, m, t ) );
            if ( resolved != null ) {
              packages.Add( resolved );
            }
          }
        }
      }

      return packages;
    }
  }
}