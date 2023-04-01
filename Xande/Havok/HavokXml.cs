using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Dalamud.Logging;

namespace Xande.Havok;

public class HavokXml {
    private XmlDocument _document;
    private XmlElement  _skeleton;

    public HavokXml( string xml ) {
        _document = new XmlDocument();
        _document.LoadXml( xml );

        _skeleton = _document.GetElementsByTagName( "object" )
           .Cast< XmlElement >()
           .First( x => x.GetAttribute( "type" ) == "hkaSkeleton" );

        if( _skeleton == null ) {
            // TODO custom exception
            throw new Exception( "Could not find skeleton" );
        }
    }

    public float[][] GetReferencePose() {
        var referencePose = _skeleton.GetElementsByTagName( "array" )
           .Cast< XmlElement >()
           .Where( x => x.GetAttribute( "name" ) == "referencePose" )
           .ToArray()[ 0 ];

        var size = int.Parse( referencePose.GetAttribute( "size" ) );

        var referencePoseArr = new float[size][];

        var i = 0;
        foreach( var node in referencePose.ChildNodes.Cast< XmlElement >() ) {
            var str = node.InnerText;
            // x00000000 <!-- 0.0 --> x00000000 <!-- 0.0 --> x00000000 <!-- 0.0 --> x3f800000 <!-- 1.0 --> x00000000 <!-- 0.0 --> x00000000 <!-- 0.0 --> x00000000 <!-- 0.0 --> x3f7fffff <!-- 1.0 --> x3f800000 <!-- 1.0 --> x3f800000 <!-- 1.0 --> x3f800000 <!-- 1.0 --> x3f800000 <!-- 1.0 -->

            // FIXME hack
            var commentRegex = new Regex( "<!--.*?-->" );
            str = commentRegex.Replace( str, "" );

            var floats = str.Split( " " )
               .Select( x => x.Trim() )
               .Where( x => !string.IsNullOrWhiteSpace( x ) )
               .Select( x => x[ 1.. ] )
               .Select( x => BitConverter.ToSingle( BitConverter.GetBytes( int.Parse( x, NumberStyles.HexNumber ) ) ) );

            referencePoseArr[ i ] = floats.ToArray();

            i += 1;
        }

        return referencePoseArr;
    }

    public int[] GetParentIndicies() {
        var parentIndicies = _skeleton.GetElementsByTagName( "array" )
           .Cast< XmlElement >()
           .Where( x => x.GetAttribute( "name" ) == "parentIndices" )
           .ToArray()[ 0 ];

        var parentIndiciesArr = new int[int.Parse( parentIndicies.GetAttribute( "size" ) )];

        var parentIndiciesStr = parentIndicies.InnerText.Split( "\n" )
           .Select( x => x.Trim() )
           .Where( x => !string.IsNullOrWhiteSpace( x ) )
           .ToArray();

        var i = 0;
        for( var j = 0; j < parentIndiciesStr.Length; j++ ) {
            var str2 = parentIndiciesStr[ j ];
            foreach( var str3 in str2.Split( " " ) ) {
                parentIndiciesArr[ i ] = int.Parse( str3 );
                i++;
            }
        }

        return parentIndiciesArr;
    }

    public string[] GetBoneNames() {
        var bonesObj = _skeleton.GetElementsByTagName( "array" )
           .Cast< XmlElement >()
           .Where( x => x.GetAttribute( "name" ) == "bones" )
           .ToArray()[ 0 ];

        var bones = new string[int.Parse( bonesObj.GetAttribute( "size" ) )];

        var boneNames = bonesObj.GetElementsByTagName( "struct" )
           .Cast< XmlElement >()
           .Select( x => x.GetElementsByTagName( "string" )
               .Cast< XmlElement >()
               .First( y => y.GetAttribute( "name" ) == "name" ) );

        var i = 0;
        foreach( var boneName in boneNames ) {
            bones[ i ] = boneName.InnerText;
            i++;
        }

        return bones;
    }

    public void DoThing() {
        var parentIndicies = GetParentIndicies();
        PluginLog.Log( "parentIndicies: " + string.Join( ", ", parentIndicies ) );

        var boneNames = GetBoneNames();
        PluginLog.Log( "boneNames: " + string.Join( ", ", boneNames ) );

        var referencePose = GetReferencePose();
    }
}