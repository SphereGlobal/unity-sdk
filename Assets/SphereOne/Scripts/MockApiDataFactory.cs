using UnityEngine;

namespace SphereOne
{
    public class MockApiDataFactory : MonoBehaviour
    {
        public static MockApiDataFactory Instance { get; set; }

        public TextAsset CredentialsJsonFile;
        public TextAsset UserJsonFile;
        public TextAsset WalletsJsonFile;
        public TextAsset BalancesJsonFile;
        public TextAsset NftsJsonFile;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("Two MockApiDataFactory instances were found, removing this one.");
                Destroy(gameObject);
                return;
            }
        }

        public string GetMockUser()
        {
            return UserJsonFile.text;
        }

        public string GetMockCredentials()
        {
            return CredentialsJsonFile.text;
        }

        public string GetMockWallets()
        {
            return WalletsJsonFile.text;
        }

        public string GetMockBalances()
        {
            return BalancesJsonFile.text;
        }

        public string GetMockNfts()
        {
            return NftsJsonFile.text;
        }
    }
}