using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SphereOne;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SphereOneWindowManager : MonoBehaviour
{
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _totalBalanceText;
    [SerializeField] GameObject _walletListParent;
    [SerializeField] GameObject _accountListParent;
    [SerializeField] GameObject _nftListParent;
    [SerializeField] GameObject _balanceLoadingCircle;
    [SerializeField] WalletListItem _walletItemPrefab;
    [SerializeField] AccountListItem _accountItemPrefab;
    [SerializeField] NftListItem _nftListItemPrefab;
    [SerializeField] List<Button> _buttons;
    [SerializeField] List<CanvasGroup> _panels;

    List<WalletListItem> _walletListItems;
    List<AccountListItem> _accountListItems;
    List<NftListItem> _nftListItems;

    CanvasGroup _myCanvas;

    void Awake()
    {
        _myCanvas = GetComponent<CanvasGroup>();

        _walletListItems = new List<WalletListItem>();
        _accountListItems = new List<AccountListItem>();
        _nftListItems = new List<NftListItem>();

        foreach (var btn in _buttons)
        {
            btn.onClick.AddListener(delegate { SelectButton(btn.name); });
        }

        DeselectAllButtons();
        SelectButton(_buttons[0].name);

        DisableAllPanels();
        EnablePanel(_buttons[0].name);

        ClearAll();
    }

    void OnEnable()
    {
        SphereOneManager.onUserLoaded += UserLoaded;
        SphereOneManager.onUserLogout += ClearAll;
        SphereOneManager.onUserWalletsLoaded += WalletsLoaded;
        SphereOneManager.onUserBalancesLoaded += AccountsLoaded;
        SphereOneManager.onUserNftsLoaded += NftsLoaded;
    }

    void OnDisable()
    {
        SphereOneManager.onUserLoaded -= UserLoaded;
        SphereOneManager.onUserLogout -= ClearAll;
        SphereOneManager.onUserWalletsLoaded -= WalletsLoaded;
        SphereOneManager.onUserBalancesLoaded -= AccountsLoaded;
        SphereOneManager.onUserNftsLoaded -= NftsLoaded;
    }

    void UserLoaded(User user)
    {
        ClearUser();

        _nameText.text = user.name;
    }

    void WalletsLoaded(List<Wallet> wallets)
    {
        ClearWallets();

        foreach (var wallet in wallets)
        {
            var item = Instantiate(_walletItemPrefab.gameObject);
            item.transform.SetParent(_walletListParent.transform, false);
            item.name = wallet.address;

            var script = item.GetComponent<WalletListItem>();
            script.Setup(wallet);

            _walletListItems.Add(script);
        }
    }

    void AccountsLoaded(List<Balance> balances)
    {
        ClearAccounts();

        _balanceLoadingCircle.SetActive(false);

        _totalBalanceText.text = "$" + SphereOneUtils.BigIntToRoundedDollarString(SphereOneManager.Instance.TotalBalance);

        foreach (var balance in balances)
        {
            var item = Instantiate(_accountItemPrefab.gameObject);
            item.transform.SetParent(_accountListParent.transform, false);
            item.name = balance.tokenMetadata.symbol;

            var script = item.GetComponent<AccountListItem>();
            script.Setup(balance);

            _accountListItems.Add(script);
        }
    }

    void NftsLoaded(List<Nft> nfts)
    {
        ClearNfts();

        foreach (var nft in nfts)
        {
            var item = Instantiate(_nftListItemPrefab.gameObject);
            item.transform.SetParent(_nftListParent.transform, false);
            item.name = nft.name;

            var script = item.GetComponent<NftListItem>();
            script.Setup(nft);

            _nftListItems.Add(script);
        }
    }

    void ClearUser()
    {
        _nameText.text = "";
    }

    void ClearAccounts()
    {
        _balanceLoadingCircle.SetActive(true);
        _totalBalanceText.text = "";

        foreach (var item in _accountListItems)
        {
            Destroy(item.gameObject);
        }

        _accountListItems.Clear();
    }

    void ClearWallets()
    {
        foreach (var item in _walletListItems)
        {
            Destroy(item.gameObject);
        }

        _walletListItems.Clear();
    }

    void ClearNfts()
    {
        foreach (var item in _nftListItems)
        {
            Destroy(item.gameObject);
        }

        _nftListItems.Clear();
    }

    void ClearAll()
    {
        ClearUser();
        ClearAccounts();
        ClearWallets();
        ClearNfts();
    }

    void DisableAllPanels()
    {
        foreach (var panel in _panels)
        {
            panel.alpha = 0;
            panel.blocksRaycasts = false;
        }
    }

    void EnablePanel(string name)
    {
        DisableAllPanels();

        foreach (var panel in _panels)
        {
            if (panel.name.Contains(name))
            {
                panel.alpha = 1;
                panel.blocksRaycasts = true;
            }
        }
    }

    void DeselectAllButtons()
    {
        foreach (var btn in _buttons)
        {
            btn.GetComponent<Image>().color = Color.clear;
            btn.GetComponentInChildren<TMP_Text>().color = Color.white;
        }
    }

    void SelectButton(string name)
    {
        DeselectAllButtons();

        foreach (var btn in _buttons)
        {
            if (btn.name == name)
            {
                ColorUtility.TryParseHtmlString("#142430", out var imgColor);
                btn.GetComponent<Image>().color = imgColor;

                ColorUtility.TryParseHtmlString("#0492e1", out var txtColor);
                btn.GetComponentInChildren<TMP_Text>().color = txtColor;

                EnablePanel(btn.name);
            }
        }
    }

    public void OpenWindow()
    {
        _myCanvas.alpha = 1;
        _myCanvas.blocksRaycasts = true;
    }

    public void CloseWindow()
    {
        _myCanvas.alpha = 0;
        _myCanvas.blocksRaycasts = false;
    }
}
