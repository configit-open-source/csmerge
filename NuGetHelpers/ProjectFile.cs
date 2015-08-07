using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

using Parsing;

namespace CsMerge.Core {
  public class ProjectFile {
    public string Name { get; private set; }

    public IReadOnlyCollection<ItemGroup> ItemGroups { get; private set; }

    public ProjectFile( string name, IReadOnlyCollection<ItemGroup> itemGroups ) {
      Name = name;
      ItemGroups = itemGroups;
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

    public Dictionary<string, TItem> GetItemsDictionary<TItem>() where TItem: Item {
      return GetItems<TItem>().ToDictionary( r => r.Key );
    }

    public IEnumerable<TItem> GetItems<TItem>() where TItem: Item {
      return ItemGroups.SelectMany( ig => ig.Items ).OfType<TItem>().Distinct();
    }

    public static void DeleteItems( XDocument document ) {
      var root = document.Root;

      Debug.Assert( root != null );

      var items = root.Descendants().Where( n => n.Parent != null && n.Parent.Name.LocalName == "ItemGroup" ).ToArray();

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