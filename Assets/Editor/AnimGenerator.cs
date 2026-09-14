using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class VariantAnimationGenerator
{
    private const string BASE_CONTROLLER_PATH = "Assets/Animations/GhostAnimator_Brown.controller";
    private const string ANIM_ROOT_DIR = "Assets/Animations";

    private static readonly string[] TARGET_FOLDERS = new string[]
    {
        "Assets/Visual Assets/EnemyWhite",
        "Assets/Visual Assets/EnemyBlack",
        "Assets/Visual Assets/EnemyGrey"
    };

    [MenuItem("Tools/Generate Enemy Animation Overrides")]
    public static void GenerateOverrides()
    {
        RuntimeAnimatorController baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BASE_CONTROLLER_PATH);
        if (baseController == null)
        {
            Debug.LogError($"[오류] Base Controller를 찾을 수 없습니다: {BASE_CONTROLLER_PATH}");
            return;
        }

        foreach (string targetFolder in TARGET_FOLDERS)
        {
            if (!Directory.Exists(targetFolder))
            {
                Debug.LogError($"[오류] 폴더가 존재하지 않습니다: {targetFolder}");
                continue;
            }

            string folderName = Path.GetFileName(targetFolder);
            string colorName = folderName.StartsWith("Enemy") ? folderName.Substring("Enemy".Length) : folderName;
            string clipOutputDir = $"{ANIM_ROOT_DIR}/{folderName}Anim";

            if (!Directory.Exists(clipOutputDir))
                Directory.CreateDirectory(clipOutputDir);

            // 1. _0 접미사 유무를 모두 포괄하여 딕셔너리에 등록
            Dictionary<string, Sprite> spriteMap = LoadNormalizedSprites(targetFolder);

            // 2. Override Controller 생성
            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            string controllerPath = $"{ANIM_ROOT_DIR}/GhostAnimator_{colorName}.overrideController";
            AssetDatabase.CreateAsset(overrideController, controllerPath);

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);

            // 3. 클립 복제 및 스프라이트 교체
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip originalClip = overrides[i].Key;
                if (originalClip == null) continue;

                AnimationClip newClip = Object.Instantiate(originalClip);
                newClip.name = $"{folderName}_{originalClip.name}";

                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(newClip);
                foreach (var binding in bindings)
                {
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(newClip, binding);
                    for (int k = 0; k < keyframes.Length; k++)
                    {
                        if (keyframes[k].value is Sprite oldSpr)
                        {
                            string targetKey = NormalizeName(oldSpr.name);

                            if (spriteMap.TryGetValue(targetKey, out Sprite newSpr))
                            {
                                keyframes[k].value = newSpr;
                            }
                            else
                            {
                                Debug.LogWarning($"[{newClip.name}] 매칭 실패 -> 원본: '{oldSpr.name}' (정규화 키: '{targetKey}')");
                            }
                        }
                    }
                    AnimationUtility.SetObjectReferenceCurve(newClip, binding, keyframes);
                }

                string clipPath = $"{clipOutputDir}/{newClip.name}.anim";
                AssetDatabase.CreateAsset(newClip, clipPath);

                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, newClip);
            }

            overrideController.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideController);
            Debug.Log($"[생성 완료] GhostAnimator_{colorName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("모든 Override Controller 및 클립 생성이 완료되었습니다.");
    }

    // 이름 뒤의 _0, _1 같은 슬라이스 인덱스를 제거하여 비교 기준을 맞춤
    private static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // 예: NormalLeft1_0 -> NormalLeft1
        if (name.EndsWith("_0"))
            return name.Substring(0, name.Length - 2);

        return name;
    }

    private static Dictionary<string, Sprite> LoadNormalizedSprites(string folderPath)
    {
        var map = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

        string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        foreach (string file in allFiles)
        {
            if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;

            string unityPath = file.Replace("\\", "/");
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(unityPath);

            foreach (var sub in subAssets)
            {
                if (sub is Sprite spr)
                {
                    // 원본 이름 그대로 등록 (NormalLeft1_0)
                    if (!map.ContainsKey(spr.name))
                        map.Add(spr.name, spr);

                    // _0을 뗀 정규화 이름도 등록 (NormalLeft1)
                    string cleanName = NormalizeName(spr.name);
                    if (!map.ContainsKey(cleanName))
                        map.Add(cleanName, spr);
                }
            }
        }

        return map;
    }
}