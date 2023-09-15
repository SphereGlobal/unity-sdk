using UnityEngine;

public class SignInCanvas : MonoBehaviour
{
    public void OnAuthReply(object value)
    {
        Debug.Log("SignInBehavior::OnAuthReply: " + value);
    }
}