using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SphereOne;

public class WalletListItem : MonoBehaviour
{
    [SerializeField] TMP_Text _walletAddress;
    [SerializeField] TMP_Text _walletChain;

    Wallet _wallet;

    void Awake()
    {
        Clear();
    }

    void Clear()
    {
        _walletAddress.text = "";
        _walletChain.text = "";
    }

    public void Setup(Wallet wallet)
    {
        _wallet = wallet;

        _walletAddress.text = _wallet.address;

        switch (_wallet.type)
        {
            case WalletType.EOA:
                _walletChain.text = _wallet.chains[0].ToString();
                break;

            case WalletType.SMARTWALLET:
                _walletChain.text = nameof(SupportedChains.POLYGON);
                break;

            default:
                Debug.LogError($"WalletType enum {_wallet.type} not supported;");
                break;
        }
    }
}
