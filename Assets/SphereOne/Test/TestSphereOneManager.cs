using System;
using UnityEngine;
using SphereOne;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.VisualScripting;

public class TestSphereOneManager : MonoBehaviour
{
    string _chargeId;

    async public void CreateCharge()
    {
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                name = "Your Item",
                image = "https://your-image-url.somewhere.com",
                amount = 2,
                quantity = 1,
            }
        };

        var chargeRequest = new ChargeReqBody
        {
            chain = SupportedChains.POLYGON,
            symbol = "MATIC",
            amount = 2,
            tokenAddress = "0x0000000000000000000000000000000000000000",
            items = chargeItems,
            successUrl = "https://your-website.com/success",
            cancelUrl = "https://your-website.com/cancel",
        };

        var isTest = false;
        var isDirectTransfer = false;
        var charge = await SphereOneManager.Instance.CreateCharge(chargeRequest, isTest, isDirectTransfer);

        _chargeId = charge.chargeId;

        // Once the user has logged in
        // if (SphereOneManager.Instance.IsAuthenticated)
        // var user = SphereOneManager.Instance.User;
    }

    async public void PayCharge()
    {
        if (_chargeId == null)
            return;

        try
        {
            var payment = await SphereOneManager.Instance.PayCharge(_chargeId);
            Debug.Log(payment.ToString());
        }
        catch (PayError e)
        {
            Debug.LogError($"An error occurred while paying the charge: {e.Message}");
            string onRampLink = e.onrampLink;
            Debug.LogError($"onRampLink: {onRampLink}");
            // Open the onRampLink in the user's default web browser
            Application.OpenURL(onRampLink);
            return;
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred while paying the charge: {e.Message}");
            return;
        }
    }

    async public void GetRouteEstimation()
    {
        if (_chargeId == null)
            return;

        try
        {
            var routeEstimation = await SphereOneManager.Instance.GetRouteEstimation(_chargeId);
            Debug.Log(routeEstimation.ToString());
        }
        catch (RouteEstimateError e)
        {
            Debug.LogError($"An error occurred while getting the route estimation: {e.Message}");
            string onRampLink = e.onrampLink;
            Debug.LogError($"onRampLink: {onRampLink}");
            // Open the onRampLink in the user's default web browser
            Application.OpenURL(onRampLink);
            return;
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred while getting the route estimation: {e.Message}");
            return;
        }
    }

#pragma warning disable CS1998 // Suppress the warning.
    // This opens a popup to enter the pin code. So, no need to await.
    async public void OpenPinCode()
    {
        if (_chargeId == null)
            return;
        SphereOneManager.Instance.OpenPinCode(_chargeId);
    }
#pragma warning restore CS1998

    async public void TransferNft()
    {
        try
        {
            // TODO: Implement this
            // await SphereOneManager.Instance.TransferNft();
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred while transferring the NFT: {e.Message}");
            return;
        }
    }
}