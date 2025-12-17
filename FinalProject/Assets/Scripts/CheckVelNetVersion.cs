// Add this to any existing script or create a quick one:
using System.Linq;
using UnityEngine;

public class CheckVelNetVersion : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== CHECKING VELNET VERSION ===");
        
        // Check package.json for version info
        string packagePath = "Packages/edu.uga.engr.vel.velnet/package.json";
        if (System.IO.File.Exists(packagePath))
        {
            string json = System.IO.File.ReadAllText(packagePath);
            Debug.Log("VelNet package.json found:");
            Debug.Log(json);
        }
        else
        {
            Debug.Log("Package path not found: " + packagePath);
        }
        
        // Check assembly version
        var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.FullName.Contains("VelNet"));
        
        if (assembly != null)
        {
            Debug.Log("VelNet Assembly: " + assembly.GetName().Version);
        }
    }
}