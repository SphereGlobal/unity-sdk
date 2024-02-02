using System;
using UnityEngine;
using SphereOne;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq; // Added this line to use LINQ methods

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

        // NOTE: This is for smart contract payments
        // var functionParams = new List<string>();
        // functionParams.Add("2000000000000000000"); // 2*10^18 (2 MATIC)

        // var smartContractPropsData = new CallSmartContractProps
        // {
        //     alias = "test",
        //     nativeValue = "0",
        //     functionParams = functionParams.ToArray()
        // };

        var isTest = false;
        var isDirectTransfer = false;
        var charge = await SphereOneManager.Instance.CreateCharge(chargeRequest, isTest, isDirectTransfer);

        // NOTE: This is for smart contract payments
        // var charge = await SphereOneManager.Instance.CreateCharge(chargeRequest, isTest, isDirectTransfer, smartContractPropsData);

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

    // This opens a popup to enter the pin code. So, no need to await.
    async public void OpenPinCodeForNftTransfer()
    {
        SphereOneManager.Instance.OpenPinCode(PincodeTargets.SendNft);
    }
#pragma warning restore CS1998

    async public void TransferNft()
    {
        try
        {
            // NOTE: receiver walletAddress. Replace this with the receiver's wallet address
            var receiver = "";
            // NOTE: current user's NFTs
            var nfts = SphereOneManager.Instance.Nfts;
            if (nfts.Count == 0)
            {
                throw new Exception("No NFTs found");
            }
            // NOTE: for testing purposes, we are just taking the first NFT that's a POLYGON BUT it has to be ERC721
            var filterNfts = nfts.Where(n => n.chain == SupportedChains.POLYGON && n.tokenType == "ERC721").ToList(); // Changed this line to filter the NFTs
            if (filterNfts.Count == 0)
            {
                throw new Exception("No NFTs found for the selected chain and token type");
            }
            var nft = filterNfts[0]; // NOTE: Change this to the NFT you want to transfer

            // NOTE: Needs to be an NFT that you/user owns
            var nftToTransfer = new NftDataParams
            {
                chain = nft.chain,
                fromAddress = nft.walletAddress, // wallet address that the NFT belongs to
                toAddress = receiver, // the receiver wallet address
                nftTokenAddress = nft.address,
                tokenId = nft.tokenId,
                reason = "Testing NFT transfer", // a reason for the transfer
            };
            var nftTransferResult = await SphereOneManager.Instance.TransferNft(nftToTransfer);
            Debug.Log("NFT transferred successfully: " + nftTransferResult.ToString());
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred while transferring the NFT: {e.Message}");
            return;
        }
    }
}