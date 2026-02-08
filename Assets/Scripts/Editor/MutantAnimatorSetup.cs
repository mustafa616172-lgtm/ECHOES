using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class MutantAnimatorSetup : EditorWindow
{
    private const string MUTANT_FOLDER = "Assets/Character/Mutant";
    private const string ANIMATOR_PATH = "Assets/Character/Mutant/MutantAnimator.controller";
    
    [MenuItem("Tools/Mutant/Setup Animator Controller")]
    public static void SetupAnimator()
    {
        // Delete existing controller if exists
        if (File.Exists(ANIMATOR_PATH.Replace("Assets/", Application.dataPath + "/")))
        {
            AssetDatabase.DeleteAsset(ANIMATOR_PATH);
        }
        
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ANIMATOR_PATH);
        
        if (controller == null)
        {
            Debug.LogError("Animator Controller could not be created!");
            return;
        }

        var rootStateMachine = controller.layers[0].stateMachine;

        // Animation states to create
        AnimatorState idleState = null;
        AnimatorState walkState = null;
        AnimatorState runState = null;
        AnimatorState roarState = null;
        AnimatorState attackState = null;
        AnimatorState deathState = null;

        // Create states with specific animations
        // IDLE - Zombie Idle
        AnimationClip idleClip = GetAnimationClipFromFBX(MUTANT_FOLDER + "/Zombie Idle.fbx");
        if (idleClip != null)
        {
            idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;
            rootStateMachine.defaultState = idleState;
            Debug.Log("[OK] Idle state created");
        }

        // WALK - Mutant Run (slower for patrol)
        AnimationClip walkClip = GetAnimationClipFromFBX(MUTANT_FOLDER + "/Mutant Run.fbx");
        if (walkClip != null)
        {
            walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;
            walkState.speed = 0.6f; // Slower for walking
            Debug.Log("[OK] Walk state created (Mutant Run at 0.6x speed)");
        }

        // RUN - Zombie Running
        AnimationClip runClip = GetAnimationClipFromFBX(MUTANT_FOLDER + "/Zombie Running.fbx");
        if (runClip != null)
        {
            runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;
            Debug.Log("[OK] Run state created");
        }

        // ROAR - Mutant Roaring
        AnimationClip roarClip = GetAnimationClipFromFBX(MUTANT_FOLDER + "/Mutant Roaring.fbx");
        if (roarClip != null)
        {
            roarState = rootStateMachine.AddState("Roar");
            roarState.motion = roarClip;
            Debug.Log("[OK] Roar state created");
        }

        // ATTACK - Mutant Swiping
        AnimationClip attackClip = GetAnimationClipFromFBX(MUTANT_FOLDER + "/Mutant Swiping.fbx");
        if (attackClip != null)
        {
            attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;
            Debug.Log("[OK] Attack state created");
        }

        // DEATH - Zombie Death
        AnimationClip deathClip = GetAnimationClipFromFBX(MUTANT_FOLDER + "/Zombie Death.fbx");
        if (deathClip != null)
        {
            deathState = rootStateMachine.AddState("Death");
            deathState.motion = deathClip;
            Debug.Log("[OK] Death state created");
        }

        // Parameters compatible with SimpleEnemyWalker
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRoaring", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        // === TRANSITIONS ===
        
        // Idle <-> Walk (Patrol)
        if (idleState != null && walkState != null)
        {
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.2f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.2f;
        }

        // Idle <-> Run (Chase)
        if (idleState != null && runState != null)
        {
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.15f;

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.15f;
        }

        // Walk <-> Run
        if (walkState != null && runState != null)
        {
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.15f;

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
            runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.15f;
        }

        // Any -> Roar (when IsRoaring = true)
        if (roarState != null)
        {
            var anyToRoar = rootStateMachine.AddAnyStateTransition(roarState);
            anyToRoar.AddCondition(AnimatorConditionMode.If, 0, "IsRoaring");
            anyToRoar.hasExitTime = false;
            anyToRoar.duration = 0.1f;
            anyToRoar.canTransitionToSelf = false;

            // Roar -> Idle (when IsRoaring = false)
            var roarToIdle = roarState.AddTransition(idleState);
            roarToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRoaring");
            roarToIdle.hasExitTime = false;
            roarToIdle.duration = 0.1f;
        }

        // Any -> Attack (trigger)
        if (attackState != null && idleState != null)
        {
            var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.1f;
            anyToAttack.canTransitionToSelf = false;

            // Attack -> Idle (after animation completes)
            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.9f;
            attackToIdle.duration = 0.1f;
        }

        // Any -> Death
        if (deathState != null)
        {
            var anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.1f;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[OK] Mutant Animator Controller created with Walk/Run/Roar/Attack states!</color>");
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
    }

    [MenuItem("Tools/Mutant/Create Mutant Prefab with Enemy AI")]
    public static void CreateMutantPrefab()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_PATH);
        
        if (controller == null)
        {
            Debug.Log("Animator Controller not found, creating...");
            SetupAnimator();
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_PATH);
        }

        string modelPath = MUTANT_FOLDER + "/T-Pose.fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        
        if (model == null)
        {
            Debug.LogError("Model not found: " + modelPath);
            return;
        }

        var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        instance.name = "Mutant_Enemy";
        
        var animator = instance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        var enemyAI = instance.GetComponent<SimpleEnemyWalker>();
        if (enemyAI == null)
        {
            enemyAI = instance.AddComponent<SimpleEnemyWalker>();
        }

        var capsule = instance.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = instance.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 1f, 0);
            capsule.radius = 0.4f;
            capsule.height = 2f;
        }

        string prefabSavePath = MUTANT_FOLDER + "/MutantEnemy.prefab";
        
        if (File.Exists(prefabSavePath.Replace("Assets/", Application.dataPath + "/")))
        {
            AssetDatabase.DeleteAsset(prefabSavePath);
        }
        
        PrefabUtility.SaveAsPrefabAsset(instance, prefabSavePath);
        Selection.activeGameObject = instance;
        
        Debug.Log("<color=green>[OK] Mutant Enemy Prefab created: " + prefabSavePath + "</color>");
        Debug.Log("<color=cyan>Character added to scene. SimpleEnemyWalker component active.</color>");
        Debug.Log("<color=yellow>NOTE: NavMesh must be baked! Window > AI > Navigation > Bake</color>");
    }

    [MenuItem("Tools/Mutant/Replace Cursed Priest with Mutant")]
    public static void ReplaceCursedPriestWithMutant()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        GameObject priestObject = null;
        
        foreach (var obj in allObjects)
        {
            string nameLower = obj.name.ToLower();
            if (nameLower.Contains("priest") || nameLower.Contains("cursed"))
            {
                Transform root = obj.transform;
                while (root.parent != null && (root.parent.name.ToLower().Contains("priest") || root.parent.name.ToLower().Contains("cursed")))
                {
                    root = root.parent;
                }
                priestObject = root.gameObject;
                break;
            }
        }

        if (priestObject == null)
        {
            Debug.LogWarning("No object containing 'Priest' or 'Cursed' found in scene.");
            return;
        }

        Vector3 position = priestObject.transform.position;
        Quaternion rotation = priestObject.transform.rotation;
        Transform parent = priestObject.transform.parent;

        var priestAI = priestObject.GetComponent<SimpleEnemyWalker>();
        
        Undo.RegisterCompleteObjectUndo(priestObject, "Replace Priest with Mutant");
        
        string prefabPath = MUTANT_FOLDER + "/MutantEnemy.prefab";
        var mutantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (mutantPrefab == null)
        {
            Debug.Log("Mutant prefab not found, creating...");
            CreateMutantPrefab();
            mutantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        GameObject newMutant = PrefabUtility.InstantiatePrefab(mutantPrefab) as GameObject;
        newMutant.name = "Mutant_Enemy";
        newMutant.transform.position = position;
        newMutant.transform.rotation = rotation;
        if (parent != null)
            newMutant.transform.SetParent(parent);

        if (priestAI != null)
        {
            var mutantAI = newMutant.GetComponent<SimpleEnemyWalker>();
            if (mutantAI != null)
            {
                EditorUtility.CopySerialized(priestAI, mutantAI);
            }
        }

        Undo.DestroyObjectImmediate(priestObject);
        Selection.activeGameObject = newMutant;
        
        Debug.Log("<color=green>[OK] Cursed Priest replaced with Mutant!</color>");
        Debug.Log("<color=cyan>New position: " + position + ", Original AI settings preserved.</color>");
    }

    [MenuItem("Tools/Mutant/Apply Animator to Scene Mutant")]
    public static void ApplyAnimatorToSceneMutant()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_PATH);
        
        if (controller == null)
        {
            Debug.LogError("Animator Controller not found: " + ANIMATOR_PATH + ". Run 'Setup Animator Controller' first.");
            return;
        }

        var mutants = GameObject.FindObjectsOfType<Animator>();
        int appliedCount = 0;

        foreach (var animator in mutants)
        {
            if (animator.gameObject.name.ToLower().Contains("mutant") || 
                animator.gameObject.name.ToLower().Contains("t-pose"))
            {
                animator.runtimeAnimatorController = controller;
                appliedCount++;
                Debug.Log("[OK] Animator applied: " + animator.gameObject.name);
            }
        }

        if (appliedCount > 0)
        {
            Debug.Log("<color=green>[OK] Animator applied to " + appliedCount + " characters.</color>");
        }
        else
        {
            Debug.LogWarning("No Mutant character found in scene. Drag the prefab to scene and try again.");
        }
    }

    [MenuItem("Tools/Mutant/Fix Mutant In Scene (Position + NavMesh + Animator)")]
    public static void FixMutantInScene()
    {
        // First ensure animator controller exists
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_PATH);
        if (controller == null)
        {
            Debug.Log("Creating Animator Controller first...");
            SetupAnimator();
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_PATH);
        }

        // Find all Mutant/T-Pose objects in scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int fixedCount = 0;

        foreach (var obj in allObjects)
        {
            string nameLower = obj.name.ToLower();
            if (!nameLower.Contains("mutant") && !nameLower.Contains("t-pose"))
                continue;
            
            // Skip child objects, only process root
            if (obj.transform.parent != null && 
                (obj.transform.parent.name.ToLower().Contains("mutant") || 
                 obj.transform.parent.name.ToLower().Contains("t-pose")))
                continue;

            Undo.RegisterCompleteObjectUndo(obj, "Fix Mutant");

            // 1. Fix Position - Lift character slightly above ground
            Vector3 pos = obj.transform.position;
            if (pos.y < 0.5f) // If character is too low
            {
                pos.y = 0f; // Set to ground level
                obj.transform.position = pos;
                Debug.Log("[Fix] Position adjusted to ground level");
            }

            // 2. Setup/Fix Animator
            var animator = obj.GetComponent<Animator>();
            if (animator == null)
            {
                animator = obj.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Debug.Log("[Fix] Animator configured");

            // 3. Setup/Fix NavMeshAgent
            var agent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
            {
                agent = obj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            }
            agent.baseOffset = 0f; // Important: prevents sinking
            agent.height = 2f;
            agent.radius = 0.4f;
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 2f;
            Debug.Log("[Fix] NavMeshAgent configured with baseOffset=0");

            // 4. Setup/Fix CapsuleCollider
            var capsule = obj.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = obj.AddComponent<CapsuleCollider>();
            }
            capsule.center = new Vector3(0, 1f, 0);
            capsule.radius = 0.4f;
            capsule.height = 2f;
            Debug.Log("[Fix] CapsuleCollider configured");

            // 5. Setup/Fix SimpleEnemyWalker
            var enemyAI = obj.GetComponent<SimpleEnemyWalker>();
            if (enemyAI == null)
            {
                enemyAI = obj.AddComponent<SimpleEnemyWalker>();
            }
            Debug.Log("[Fix] SimpleEnemyWalker attached");

            // 6. Ensure object is active
            obj.SetActive(true);

            fixedCount++;
            EditorUtility.SetDirty(obj);
        }

        if (fixedCount > 0)
        {
            Debug.Log("<color=green>[SUCCESS] Fixed " + fixedCount + " Mutant character(s)!</color>");
            Debug.Log("<color=yellow>IMPORTANT: Make sure NavMesh is baked! Window > AI > Navigation > Bake</color>");
            Debug.Log("<color=cyan>Press Play to test animations.</color>");
        }
        else
        {
            Debug.LogWarning("No Mutant/T-Pose object found in scene.");
            Debug.Log("Drag 'Assets/Character/Mutant/T-Pose.fbx' into scene and run this again.");
        }
    }

    private static AnimationClip GetAnimationClipFromFBX(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        
        return null;
    }
}
