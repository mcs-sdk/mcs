using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MCS;
using MCS_Utilities.MorphExtraction;
using MCS.FOUNDATIONS;
using MCS.SERVICES;


namespace MCS.Utility
{
    public static class MCSResourceConverter
    {
        [Obsolete("Please switch to MorphData")]
        public static MCS_Utilities.MorphExtraction.Structs.BlendshapeState UncompressedToBlendshapeState(ref byte[] bytes)
        {
            return StreamingMorphs.ConvertBytesToBlendshapeState(ref bytes);
        }
        [Obsolete("Please switch to MorphData")]
        public static MCS_Utilities.MorphExtraction.Structs.BlendshapeState CompressedToBlendshapeState(ref byte[] bytes)
        {
            return StreamingMorphs.DecompressAndConvertBytesToBlendshapeState(ref bytes);
        }
    }
}
