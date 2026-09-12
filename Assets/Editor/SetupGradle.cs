using UnityEditor;
using System;
using System.Reflection;
using UnityEngine;

public static class SetupGradle
{
    public static void DoIt()
    {
        Debug.Log("===> Running SetupGradle.DoIt()");
        Type resolver = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
        if (resolver != null) {
            Debug.Log("===> Found PlayServicesResolver!");
            var resolveMethod = resolver.GetMethod("ResolveSync", BindingFlags.Public | BindingFlags.Static);
            if (resolveMethod != null) {
                Debug.Log("===> Found ResolveSync! Invoking...");
                resolveMethod.Invoke(null, new object[] { true });
                Debug.Log("===> Finished Invoking ResolveSync!");
            } else {
                Debug.LogError("===> ResolveSync method NOT FOUND!");
            }
        } else {
            Debug.LogError("===> PlayServicesResolver class NOT FOUND in Google.JarResolver!");
        }
    }
}
