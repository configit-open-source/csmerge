using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

using NLog;

namespace CsMerge.Core {
  public class ProjectFile {
    public string Name { get; private set; }

    public IReadOnlyCollection<ItemGroup> ItemGroups { get; private set; }

    public ProjectFile( string name, IReadOnlyCollection<ItemGroup> itemGroups ) {
      Name = name;
      ItemGroups = itemGroups;
    }


    private static void RemovePackageReferences( string packagesPrefix, XDocument document ) {
      var logger = LogManager.GetCurrentClassLogger();
      //logger.Debug( "Removing nuget references from " + document.ToString() );

      var root = document.Root;
      if ( root == null ) {
        throw new ArgumentException( "Invalid csproj file" );
      }
      var references = root.Descendants( root.Name.Namespace.GetName( "Reference" ) ).ToArray();

      foreach ( var reference in references ) {
        var hintPath = reference.Elements( reference.Name.Namespace.GetName( "HintPath" ) ).FirstOrDefault();
        if ( hintPath == null ) {
          continue;
        }
        if ( hintPath.Value.StartsWith( packagesPrefix ) ) {
          logger.Debug( "Removing reference with hintpath " + hintPath.Value );
          reference.Remove();
        }
      }
    }
      
    public static void AddItems( XDocument doc, Item[] items ) {
      var root = doc.Root;
      var itemGroupName = root.Name.Namespace.GetName( "ItemGroup" );

      foreach ( var itemGroup in items.GroupBy( r => r.Action ).OrderBy( g => g.Key ) ) {

        var newGroup = new XElement( itemGroupName );

        foreach ( var item in itemGroup.OrderBy( i => i.Key ) ) {
          newGroup.Add( item.ToElement( root.Name.Namespace ) );
        }

        root.Add( newGroup );
      }
    }

    public static void DeleteItems( XDocument document ) {
      var root = document.Root;

      Debug.Assert( root != null );

      var items = root.Descendants()
        .Where( n => n.Parent.Name.LocalName == "ItemGroup" ).ToArray();

      foreach ( var e in items ) {
        e.Remove();
      }

      foreach ( var itemGroup in root.Elements( root.Name.Namespace.GetName( "ItemGroup" ) )
                                     .Where( itemGroup => itemGroup.IsEmpty ).ToArray() ) {
        itemGroup.Remove();
      }
    }
  }
}