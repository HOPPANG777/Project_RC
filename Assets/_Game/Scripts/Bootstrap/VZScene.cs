using UnityEngine;

//==============================================================================
// VZScene
//==============================================================================
// Scene bootstrapper that creates the single project-wide GameRoot.
//==============================================================================
public static class VZScene
{
    // Creates GameRoot after the scene loads if no root exists yet.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<GameRoot>() != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("GameRoot");
        rootObject.AddComponent<GameRoot>();
    }
}
