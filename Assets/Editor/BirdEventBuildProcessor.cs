using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

class BirdEventBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            BirdEvent[] events = roots[i].GetComponentsInChildren<BirdEvent>(true);
            for (int e = 0; e < events.Length; e++)
            {
                AnimationClip clip = events[e].PrepareFlapClipForPlayer();
                if (clip == null)
                {
                    Debug.LogWarning("BirdEvent: no flap clip found while preparing " + scene.name + " for play or build.");
                }
            }
        }
    }
}
