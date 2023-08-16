using UnityEngine;
using SphereOne;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                amount = 0.9,
                quantity = 1,
            }
        };

        var chargeRequest = new ChargeReqBody
        {
            chain = SupportedChains.SOLANA,
            symbol = "SOL",
            amount = 0.9,
            tokenAddress = "So11111111111111111111111111111111111111112",
            items = chargeItems,
            successUrl = "https://your-website.com/success",
            cancelUrl = "https://your-website.com/cancel",
        };

        var charge = await SphereOneManager.Instance.CreateCharge(chargeRequest);

        if (charge == null)
            return;

        _chargeId = charge.chargeId;

        Debug.Log(charge.ToString());
    }

    async public void PayCharge()
    {
        if (_chargeId == null)
            return;

        var payment = await SphereOneManager.Instance.PayCharge(_chargeId);
    }
}