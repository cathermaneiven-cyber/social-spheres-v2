using System;

namespace SocialSpheres.ModIO
{
    [Serializable]
    public class ModPage
    {
        public ModProfile[] data;
        public int result_count;
        public int result_total;
        public int result_limit;
        public int result_offset;
    }

    [Serializable]
    public class ModProfile
    {
        public int id;
        public int game_id;
        public string name;
        public string summary;
        public string description_plaintext;
        public ModLogo logo;
        public ModUser submitted_by;
        public ModStats stats;
        public ModFile modfile;
        public ModTag[] tags;
    }

    [Serializable]
    public class ModLogo
    {
        public string thumb_320x180;
        public string thumb_640x360;
        public string original;
    }

    [Serializable]
    public class ModUser
    {
        public int id;
        public string username;
    }

    [Serializable]
    public class ModStats
    {
        public int mod_id;
        public int downloads_total;
        public int subscribers_total;
        public float ratings_weighted_aggregate;
        public int ratings_total;
    }

    [Serializable]
    public class ModFile
    {
        public int id;
        public long filesize;
        public string version;
        public ModFileDownload download;
    }

    [Serializable]
    public class ModFileDownload
    {
        public string binary_url;
        public string date_expires;
    }

    [Serializable]
    public class ModTag
    {
        public string name;
    }
}
