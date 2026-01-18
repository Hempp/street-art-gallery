using System;
using System.Collections.Generic;
using UnityEngine;

namespace GalleryVR
{
    [Serializable]
    public class ArtworkInfo
    {
        public int id;
        public string title;
        public string artist;
        public string year;
        public string description;
        public string panel_name;
        public string hotspot_name;
        public string artwork_name;
        public string texture;
    }

    [Serializable]
    public class ArtworkDatabase
    {
        public List<ArtworkInfo> artworks;
    }

    public class ArtworkData : MonoBehaviour
    {
        public static ArtworkData Instance { get; private set; }

        [SerializeField] private TextAsset artworkJson;

        public ArtworkDatabase Database { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadDatabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadDatabase()
        {
            if (artworkJson != null)
            {
                Database = JsonUtility.FromJson<ArtworkDatabase>(artworkJson.text);
                Debug.Log($"Loaded {Database.artworks.Count} artworks");
            }
        }

        public ArtworkInfo GetArtworkById(int id)
        {
            return Database?.artworks.Find(a => a.id == id);
        }

        public ArtworkInfo GetArtworkByPanelName(string panelName)
        {
            return Database?.artworks.Find(a => a.panel_name == panelName);
        }
    }
}
