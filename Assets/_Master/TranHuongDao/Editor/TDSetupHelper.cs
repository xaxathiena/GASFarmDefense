using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using Abel.TranHuongDao.Core;
using GAS;

/// <summary>
/// One-shot setup helper for TranHuongDao TD scene.
/// Run Abel â†’ TranHuongDao â†’ Full Scene Setup to do everything at once.
/// </summary>
public static class TDSetupHelper
{
    // â”€â”€ Asset paths â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private const string SoFolder      = "Assets/_Master/TranHuongDao/SO";
    private const string AttackSoPath  = SoFolder + "/TD_TowerNormalAttack.asset";
    private const string DatabasePath  = "Assets/_Master/Render2D/UnitRender/Datas/UnitsDatabase.asset";
    private const string DamageEffPath = "Assets/_Master/GAS/SO/Effects/DameEffect.asset";

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [MenuItem("Abel/TranHuongDao/Add GameLifetimeScope Component")]
    public static void AddGameLifetimeScopeComponent()
    {
        var go = GameObject.Find("GameLifetimeScope");
        if (go == null) { Debug.LogError("[TDSetup] 'GameLifetimeScope' GO not found."); return; }

        // Resolve the correct type by full name to avoid ambiguity
        var correctType = ResolveType("Abel.TranHuongDao.Core.GameLifetimeScope");
        if (correctType == null)
        {
            Debug.LogError("[TDSetup] Cannot resolve Abel.TranHuongDao.Core.GameLifetimeScope — compile errors?");
            return;
        }

        // Check if the correct component is already on the GO
        var existing = go.GetComponent(correctType);
        if (existing != null)
        {
            Debug.Log("[TDSetup] GameLifetimeScope component already present correctly.");
            EditorSceneManager.MarkSceneDirty(go.scene);
            return;
        }

        // Remove ANY component that derives from LifetimeScope (stale or wrong type)
        var lifetimeScopeType = typeof(VContainer.Unity.LifetimeScope);
        foreach (var comp in go.GetComponents<Component>())
        {
            if (comp != null && lifetimeScopeType.IsAssignableFrom(comp.GetType()))
            {
                Debug.Log($"[TDSetup] Removing stale component: {comp.GetType().FullName}");
                UnityEngine.Object.DestroyImmediate(comp);
                break;
            }
        }

        go.AddComponent(correctType);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log($"[TDSetup] Added {correctType.FullName} component.");
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [MenuItem("Abel/TranHuongDao/Create Tower Normal Attack SO")]
    public static TDTowerNormalAttackData CreateAttackSO()
    {
        if (!AssetDatabase.IsValidFolder(SoFolder))
            AssetDatabase.CreateFolder("Assets/_Master/TranHuongDao", "SO");

        var existing = AssetDatabase.LoadAssetAtPath<TDTowerNormalAttackData>(AttackSoPath);
        if (existing != null)
        {
            Debug.Log("[TDSetup] TD_TowerNormalAttack.asset already exists.");
            return existing;
        }

        var so = ScriptableObject.CreateInstance<TDTowerNormalAttackData>();
        so.abilityName         = "Tower Normal Attack";
        so.attackRange         = 8f;
        so.damageAmount        = 20f;
        so.bulletSpeed         = 12f;
        so.collisionThreshold  = 0.35f;

        // Wire existing DameEffect if found
        var damageEff = AssetDatabase.LoadAssetAtPath<GameplayEffect>(DamageEffPath);
        if (damageEff != null)
        {
            so.damageEffect = damageEff;
            Debug.Log("[TDSetup] Wired DameEffect â†’ damageEffect.");
        }

        AssetDatabase.CreateAsset(so, AttackSoPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[TDSetup] Created {AttackSoPath}");
        return so;
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [MenuItem("Abel/TranHuongDao/Wire All Scene References")]
    public static void WireAllSceneReferences()
    {
        // â”€â”€ Load assets â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var db        = AssetDatabase.LoadAssetAtPath<Abel.TowerDefense.Config.UnitRenderDatabase>(DatabasePath);
        var attackSO  = AssetDatabase.LoadAssetAtPath<TDTowerNormalAttackData>(AttackSoPath)
                     ?? CreateAttackSO();

        // â”€â”€ Find GOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var grm       = FindOrWarn<Abel.TowerDefense.Render.GameRenderManager>("GameRenderManager");
        var debugger  = FindOrWarn<Abel.TowerDefense.DebugTools.UnitDebugger>("UnitDebugger");

        // Resolve scope by full name to get the namespaced type
        var correctType = ResolveType("Abel.TranHuongDao.Core.GameLifetimeScope");
        if (correctType == null) { Debug.LogError("[TDSetup] Cannot resolve GameLifetimeScope type."); return; }
        var scopeGo = GameObject.Find("GameLifetimeScope");
        if (scopeGo == null) { Debug.LogWarning("[TDSetup] GO 'GameLifetimeScope' not found."); return; }
        var scopeComp = scopeGo.GetComponent(correctType) as Component;
        if (scopeComp == null) { Debug.LogWarning("[TDSetup] Correct GameLifetimeScope component not on GO — run Add Component first."); return; }

        if (grm == null) return;

        // â”€â”€ GameRenderManager â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (db != null)
        {
            grm.gameDatabase = db;
            EditorUtility.SetDirty(grm);
            Debug.Log("[TDSetup] GameRenderManager.gameDatabase â†’ UnitsDatabase");
        }

        // â”€â”€ GameLifetimeScope via SerializedObject â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var so2 = new SerializedObject(scopeComp);
        so2.Update();
        // ── GameLifetimeScope via Reflection ──────────────────────────────────────
        SetPrivateField(scopeComp, "unitDatabase",          db);
        SetPrivateField(scopeComp, "renderManager",         grm);
        SetPrivateField(scopeComp, "unitDebugger",          debugger);
        SetPrivateField(scopeComp, "towerNormalAttackData", attackSO);
        so2.ApplyModifiedProperties();
        EditorUtility.SetDirty(scopeComp);
        Debug.Log("[TDSetup] All Inspector references wired.");

        EditorSceneManager.MarkSceneDirty(scopeComp.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [MenuItem("Abel/TranHuongDao/Full Scene Setup")]
    public static void FullSceneSetup()
    {
        AddGameLifetimeScopeComponent();
        CreateAttackSO();
        WireAllSceneReferences();
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        Debug.Log("[TDSetup] âœ“ Full scene setup complete.");
    }
    // ────────────────────────────────────────────────────────────────────────────
    [MenuItem("Abel/TranHuongDao/Debug Scope Type")]
    public static void DebugScopeType()
    {
        var scope = FindOrWarn<GameLifetimeScope>("GameLifetimeScope");
        if (scope == null) return;

        Debug.Log($"[TDSetup] scope.GetType().FullName  = {scope.GetType().FullName}");
        Debug.Log($"[TDSetup] typeof(GameLifetimeScope).FullName = {typeof(GameLifetimeScope).FullName}");

        // Enumerate ALL GameLifetimeScope types in Assembly-CSharp
        var asm = System.Reflection.Assembly.Load("Assembly-CSharp");
        if (asm != null)
        {
            var matches = System.Array.FindAll(asm.GetTypes(), t2 => t2.Name == "GameLifetimeScope");
            foreach (var m in matches)
                Debug.Log($"[TDSetup] Assembly-CSharp type: {m.FullName}  fields={m.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly).Length}");
        }
    }

    [MenuItem("Abel/TranHuongDao/Force Recompile")]
    public static void ForceRecompile()
    {
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(
            UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache);
        Debug.Log("[TDSetup] Requested clean script recompilation.");
    }

    [MenuItem("Abel/TranHuongDao/List All Assemblies")]
    public static void ListAllAssemblies()
    {
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            if (asm.FullName.StartsWith("Assembly-CSharp") || asm.FullName.StartsWith("mscorlib") || asm.FullName.StartsWith("System"))
                Debug.Log($"[TDSetup] Assembly: {asm.FullName}");
            var t = asm.GetType("Abel.TranHuongDao.Core.GameLifetimeScope");
            if (t != null) Debug.Log($"[TDSetup] FOUND in {asm.FullName}");
        }

        // Try direct assembly lookup
        try
        {
            var csharpAsm = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            if (csharpAsm == null) { Debug.Log("[TDSetup] Assembly-CSharp NOT FOUND in AppDomain!"); return; }
            var types = csharpAsm.GetTypes();
            var allGameLifetime = System.Array.FindAll(types, t2 => t2.Name == "GameLifetimeScope");
            Debug.Log($"[TDSetup] All GameLifetimeScope in Assembly-CSharp ({allGameLifetime.Length}):");
            foreach (var gt in allGameLifetime) Debug.Log($"  - {gt.FullName}  fields={gt.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly).Length}");

            // Also check if TranHuongDao namespace types are there
            var tranTypes = System.Array.FindAll(types, t2 => (t2.Namespace ?? "").Contains("TranHuongDao"));
            Debug.Log($"[TDSetup] TranHuongDao types count: {tranTypes.Length}");
            foreach (var tt in tranTypes) Debug.Log($"  - {tt.FullName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TDSetup] GetTypes failed: {ex.Message}");
        }
    }
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private static System.Type ResolveType(string fullName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }

    private static T FindOrWarn<T>(string goName) where T : Component
    {
        var go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning($"[TDSetup] GO '{goName}' not found in scene."); return null; }
        var c = go.GetComponent<T>();
        if (c == null) Debug.LogWarning($"[TDSetup] Component {typeof(T).Name} not on '{goName}'.");
        return c;
    }

    private static void SetRef2(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogWarning($"[TDSetup] Prop '{propName}' not found in SerializedObject."); return; }
        prop.objectReferenceValue = value;
        Debug.Log($"[TDSetup] Set {propName} = {value?.name ?? "null"}");
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        // First try the runtime type chain
        var type = obj.GetType();
        System.Reflection.FieldInfo fi = null;
        while (type != null && fi == null)
        {
            fi = type.GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            type = type.BaseType;
        }
        // Fallback: try the explicitly resolved namespaced type
        if (fi == null)
        {
            var resolvedType = ResolveType("Abel.TranHuongDao.Core.GameLifetimeScope");
            if (resolvedType != null)
            {
                var t2 = resolvedType;
                while (t2 != null && fi == null)
                {
                    fi = t2.GetField(fieldName,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public);
                    t2 = t2.BaseType;
                }
            }
        }
        if (fi == null) { Debug.LogWarning($"[TDSetup] Field '{fieldName}' not found via Reflection."); return; }
        fi.SetValue(obj, value);
        Debug.Log($"[TDSetup] Set {fieldName} = {value}");
    }

    private static void SetRef(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogWarning($"[TDSetup] Property '{propName}' not found."); return; }
        prop.objectReferenceValue = value;
    }
}

