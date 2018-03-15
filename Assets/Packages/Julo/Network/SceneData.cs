using System.Collections;

using UnityEngine;

namespace Julo.Network
{
    [System.Serializable]
    public class SceneData
    {
        public string assetName;

        public string englishName;
        public string spanishName;

        public int minPlayers;
        public int maxPlayers;

        public int minSize;
        public int maxSize;
        public int defaultSize;
    }

}
