using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SphereOne;

public class NftListItem : MonoBehaviour
{
    [SerializeField] RawImage _img;
    [SerializeField] TMP_Text _name;

    Nft _nft;

    void Awake()
    {
        Clear();
    }

    void Clear()
    {
        _img.color = Color.clear;
        _name.text = "";
    }

    public void Setup(Nft nft)
    {
        _nft = nft;

        _name.text = nft.name;

        // Fetch img        
        try
        {
            // Getting cors issue while fetching
            // Texture2D _texture;
            // _texture = await WebRequestWrapper.GetRemoteTexture(nft.img);
            // _texture.Apply();
            // _img.texture = _texture;
            // _img.color = Color.white;
        }
        catch
        {

        }
    }
}
