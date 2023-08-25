using UnityEngine;

namespace SphereOne
{
    public enum LoginBehavior
    {
        // Disabling slideout mode for now. Ory iframe is broken
        [InspectorName(null)]
        SLIDEOUT,
        POPUP
    }
}
