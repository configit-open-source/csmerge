using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cpc.CsMerge.Core {
  /// <summary>
  /// 
  /// </summary>
  public sealed class PackageVersion : IComparable<PackageVersion>, IComparable {
    public List<uint> VersionComponents { get; set; }

    private static readonly Regex parseEx =
      new Regex(
        @"^(?<major>\d+)(\.(?<component>\d+))*"
        + @"(\-(?<pre>[0-9A-Za-z\-\.]+))?"
        + @"(\+(?<build>[0-9A-Za-z\-\.]+))?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture );

    public static readonly PackageVersion MinValue = new PackageVersion( new int[] { } );
    public static readonly PackageVersion MaxValue = new PackageVersion {
      VersionComponents = new List<uint> { uint.MaxValue }
    };

    /// <summary>
    ///   Initializes a new instance of the <see cref="PackageVersion" /> class.
    /// </summary>
    public PackageVersion( IEnumerable<int> versionComponents, string prerelease = null, string build = null ) {
      VersionComponents = versionComponents.Select( i => (uint) i ).ToList();

      // strings are interned to be able to compare by reference in equals method
      Prerelease = string.Intern( prerelease ?? "" );
      Build = string.Intern( build ?? "" );
    }

    public PackageVersion( params int[] versionComponents )
      : this( versionComponents, null, null ) {
    }

    /// <summary>
    ///   Parses the specified string to a semantic version.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <returns>The SemVersion object.</returns>
    /// <exception cref="System.InvalidOperationException">When a invalid version string is passed.</exception>
    public static PackageVersion Parse( string version ) {
      var match = parseEx.Match( version );
      if ( !match.Success ) {
        throw new ArgumentException( "Invalid version.", "version" );
      }

      CaptureCollection capturedComponents = match.Groups["component"].Captures;

      IEnumerable<Capture> versionComponentCaptures = match.Groups["major"].Captures.Cast<Capture>().Concat<Capture>( capturedComponents.Cast<Capture>() );

      int[] versionComponents = versionComponentCaptures.Select( c => int.Parse( c.Value ) ).ToArray();
      string prerelease = null;
      string build = null;

      if ( match.Groups["pre"].Success ) {
        prerelease = match.Groups["pre"].Value;
      }

      if ( match.Groups["build"].Success ) {
        build = match.Groups["build"].Value;
      }

      return new PackageVersion( versionComponents, prerelease, build );
    }

    /// <summary>
    ///   Parses the specified string to a semantic version.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <param name="semver">
    ///   When the method returns, contains a SemVersion instance equivalent
    ///   to the version string passed in, if the version string was valid, or <c>null</c> if the
    ///   version string was not valid.
    /// </param>
    /// <returns><c>False</c> when a invalid version string is passed, otherwise <c>true</c>.</returns>
    public static bool TryParse( string version, out PackageVersion semver ) {
      try {
        semver = Parse( version );
        return true;
      }
      catch ( Exception ) {
        semver = null;
        return false;
      }
    }

    /// <summary>
    ///   Tests the specified versions for equality.
    /// </summary>
    /// <param name="versionA">The first version.</param>
    /// <param name="versionB">The second version.</param>
    /// <returns>If versionA is equal to versionB <c>true</c>, else <c>false</c>.</returns>
    public static bool Equals( PackageVersion versionA, PackageVersion versionB ) {
      if ( ReferenceEquals( versionA, null ) ) {
        return ReferenceEquals( versionB, null );
      }
      return versionA.Equals( versionB );
    }

    /// <summary>
    ///   Compares the specified versions.
    /// </summary>
    /// <param name="versionA">The version to compare to.</param>
    /// <param name="versionB">The version to compare against.</param>
    /// <returns>
    ///   If versionA &lt; versionB <c>-1</c>, if versionA &gt; versionB <c>1</c>,
    ///   if versionA is equal to versionB <c>0</c>.
    /// </returns>
    public static int Compare( PackageVersion versionA, PackageVersion versionB ) {
      if ( ReferenceEquals( versionA, null ) ) {
        return ReferenceEquals( versionB, null ) ? 0 : -1;
      }
      return versionA.CompareTo( versionB );
    }


    /// <summary>
    ///   Gets the pre-release version.
    /// </summary>
    /// <value>
    ///   The pre-release version.
    /// </value>
    public string Prerelease { get; private set; }

    /// <summary>
    ///   Gets the build version.
    /// </summary>
    /// <value>
    ///   The build version.
    /// </value>
    public string Build { get; private set; }

    /// <summary>
    ///   Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///   A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString() {
      var version = "";

      version = string.Join( ".", VersionComponents );
      
      if ( !string.IsNullOrEmpty( Prerelease ) ) {
        version += "-" + Prerelease;
      }
      if ( !string.IsNullOrEmpty( Build ) ) {
        version += "+" + Build;
      }
      return version;
    }

    /// <summary>
    ///   Compares the current instance with another object of the same type and returns an integer that indicates
    ///   whether the current instance precedes, follows, or occurs in the same position in the sort order as the
    ///   other object.
    /// </summary>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <returns>
    ///   A value that indicates the relative order of the objects being compared.
    ///   The return value has these meanings: Value Meaning Less than zero
    ///   This instance precedes <paramref name="obj" /> in the sort order.
    ///   Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. i
    ///   Greater than zero This instance follows <paramref name="obj" /> in the sort order.
    /// </returns>
    public int CompareTo( object obj ) {
      return CompareTo( (PackageVersion)obj );
    }

    /// <summary>
    ///   Compares the current instance with another object of the same type and returns an integer that indicates
    ///   whether the current instance precedes, follows, or occurs in the same position in the sort order as the
    ///   other object.
    /// </summary>
    /// <param name="other">An object to compare with this instance.</param>
    /// <returns>
    ///   A value that indicates the relative order of the objects being compared.
    ///   The return value has these meanings: Value Meaning Less than zero
    ///   This instance precedes <paramref name="other" /> in the sort order.
    ///   Zero This instance occurs in the same position in the sort order as <paramref name="other" />. i
    ///   Greater than zero This instance follows <paramref name="other" /> in the sort order.
    /// </returns>
    public int CompareTo( PackageVersion other ) {
      if ( ReferenceEquals( other, null ) ) {
        return 1;
      }

      var r = CompareByPrecedence( other );
      if ( r != 0 ) {
        return r;
      }

      r = CompareComponent( Build, other.Build );
      return r;
    }

    /// <summary>
    ///   Compares to semantic versions by precedence. This does the same as a Equals, but ignores the build information.
    /// </summary>
    /// <param name="other">The semantic version.</param>
    /// <returns><c>true</c> if the version precedence matches.</returns>
    public bool PrecedenceMatches( PackageVersion other ) {
      return CompareByPrecedence( other ) == 0;
    }

    /// <summary>
    ///   Compares to semantic versions by precedence. This does the same as a Equals, but ignores the build information.
    /// </summary>
    /// <param name="other">The semantic version.</param>
    /// <returns>
    ///   A value that indicates the relative order of the objects being compared.
    ///   The return value has these meanings: Value Meaning Less than zero
    ///   This instance precedes <paramref name="other" /> in the version precedence.
    ///   Zero This instance has the same precedence as <paramref name="other" />. i
    ///   Greater than zero This instance has creater precedence as <paramref name="other" />.
    /// </returns>
    public int CompareByPrecedence( PackageVersion other ) {
      if ( ReferenceEquals( other, null ) ) {
        return 1;
      }

      var thisLastNonZero = VersionComponents.FindLastIndex( i => i > 0 );
      var otherLastNonZero = other.VersionComponents.FindLastIndex( i => i > 0 );

      var compareLimit = Math.Min( thisLastNonZero, otherLastNonZero );

      for ( int i = 0; i <= compareLimit; i++ ) {
        int r = VersionComponents[i].CompareTo( other.VersionComponents[i] );
        if ( r != 0 ) {
          return r;
        }
      }

      if ( thisLastNonZero > otherLastNonZero ) {
        return 1;
      }

      if ( otherLastNonZero > thisLastNonZero ) {
        return -1;
      }

      return CompareComponent( Prerelease, other.Prerelease, true );
    }

    private static int CompareComponent( string a, string b, bool lower = false ) {
      var aEmpty = string.IsNullOrEmpty( a );
      var bEmpty = string.IsNullOrEmpty( b );
      if ( aEmpty && bEmpty ) {
        return 0;
      }

      if ( aEmpty ) {
        return lower ? 1 : -1;
      }
      if ( bEmpty ) {
        return lower ? -1 : 1;
      }

      var aComps = a.Split( '.' );
      var bComps = b.Split( '.' );

      var minLen = Math.Min( aComps.Length, bComps.Length );
      for ( var i = 0; i < minLen; i++ ) {
        var ac = aComps[i];
        var bc = bComps[i];
        int anum, bnum;
        var isanum = int.TryParse( ac, out anum );
        var isbnum = int.TryParse( bc, out bnum );
        if ( isanum && isbnum ) {
          return anum.CompareTo( bnum );
        }
        if ( isanum ) {
          return -1;
        }
        if ( isbnum ) {
          return 1;
        }
        var r = string.CompareOrdinal( ac, bc );
        if ( r != 0 ) {
          return r;
        }
      }

      return aComps.Length.CompareTo( bComps.Length );
    }

    /// <summary>
    ///   Determines whether the specified <see cref="System.Object" /> is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
    /// <returns>
    ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals( object obj ) {
      if ( ReferenceEquals( obj, null ) ) {
        return false;
      }

      if ( ReferenceEquals( this, obj ) ) {
        return true;
      }

      var other = (PackageVersion) obj;

      // do string comparison by reference (possible because strings are interned in ctor)
      return VersionComponents.SequenceEqual( other.VersionComponents )
             && ReferenceEquals( Prerelease, other.Prerelease ) && ReferenceEquals( Build, other.Build );
    }

    /// <summary>
    ///   Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///   A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode() {
      unchecked {
        var result = 1;
        for ( int i = 0; i < VersionComponents.Count; i++ ) {
          result *= result * 31 + i;
        }
        result = result * 31 + Prerelease.GetHashCode();
        result = result * 31 + Build.GetHashCode();
        return result;
      }
    }

    /// <summary>
    ///   Implicit conversion from string to SemVersion.
    /// </summary>
    /// <param name="version">The semantic version.</param>
    /// <returns>The SemVersion object.</returns>
    public static implicit operator PackageVersion( string version ) {
      return Parse( version );
    }

    /// <summary>
    ///   The override of the equals operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>If left is equal to right <c>true</c>, else <c>false</c>.</returns>
    public static bool operator ==( PackageVersion left, PackageVersion right ) {
      return Equals( left, right );
    }

    /// <summary>
    ///   The override of the un-equal operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>If left is not equal to right <c>true</c>, else <c>false</c>.</returns>
    public static bool operator !=( PackageVersion left, PackageVersion right ) {
      return !Equals( left, right );
    }

    /// <summary>
    ///   The override of the greater operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>If left is greater than right <c>true</c>, else <c>false</c>.</returns>
    public static bool operator >( PackageVersion left, PackageVersion right ) {
      return Compare( left, right ) == 1;
    }

    /// <summary>
    ///   The override of the greater than or equal operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>If left is greater than or equal to right <c>true</c>, else <c>false</c>.</returns>
    public static bool operator >=( PackageVersion left, PackageVersion right ) {
      return left == right || left > right;
    }

    /// <summary>
    ///   The override of the less operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>If left is less than right <c>true</c>, else <c>false</c>.</returns>
    public static bool operator <( PackageVersion left, PackageVersion right ) {
      return Compare( left, right ) == -1;
    }

    /// <summary>
    ///   The override of the less than or equal operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>If left is less than or equal to right <c>true</c>, else <c>false</c>.</returns>
    public static bool operator <=( PackageVersion left, PackageVersion right ) {
      return left == right || left < right;
    }
  }
}