using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NLog;

namespace Cpc.CsMerge.Core {
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
  }
}