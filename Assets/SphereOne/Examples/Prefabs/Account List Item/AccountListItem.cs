using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SphereOne;

public class AccountListItem : MonoBehaviour
{
    [SerializeField] RawImage _logo;
    [SerializeField] TMP_Text _currencySymbol;
    [SerializeField] TMP_Text _amountOfTokens;
    [SerializeField] TMP_Text _usdValue;

    Balance _balance;

    void Awake()
    {
        Clear();
    }

    void Clear()
    {
        _logo.color = Color.clear;
        _currencySymbol.text = "";
        _amountOfTokens.text = "";
        _usdValue.text = "";
    }

    public void Setup(Balance balance)
    {
        _balance = balance;

        _currencySymbol.text = balance.tokenMetadata.symbol;

        var numOfTokens = SphereOneUtils.BigIntToBigDecimal(balance.amount, balance.tokenMetadata.decimals);

        var rounded = string.Format("{0:0.####}", (decimal)numOfTokens);
        _amountOfTokens.text = $"{rounded} {balance.tokenMetadata.symbol}";

        var usdValue = SphereOneUtils.BigIntToRoundedDollarString(balance.price);

        _usdValue.text = $"${usdValue}";

        // TODO Fetch img (store imgs locally)
    }
}
