#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GAS;
using Abel.TranHuongDao.Core.Abilities;

namespace Abel.TranHuongDao.Editor
{
    public static class BulkAbilitiesAssetCreator
    {
        private const string FOLDER_PATH = "Assets/_Master/TranHuongDao/Core/Abilities/Data";

        [MenuItem("GAS Farm Defense/Procs/Create All 20 Default Abnormal Abilities")]
        public static void CreateAllDefaultAbilities()
        {
            EnsureFolderExists();

            // 1. Kích hoạt Hoàng Thượng, tăng tốc đánh 10% trong phạm vi 500
            CreateBuffProc("HoangThuong_AtkSpeed", 100f, EProcTriggerCondition.OnAttackStart, "AttackCooldown", 0.9f, 500f, 5f);

            // 2. Mắt diều hâu: 30% tạo phép chú lên đồng minh tăng 20% tốc đánh (Giả sử buff vào bản thân/đồng minh gần)
            CreateBuffProc("MatDieuHau_AtkSpeedBuff", 30f, EProcTriggerCondition.OnAttackStart, "AttackCooldown", 0.8f, 0f, 5f, EProcContextTarget.Source);

            // 3. 5% đóng băng kẻ thù 2.5s, sát thương 50
            CreateCCProc("DongBang_50Dmg", 5f, 50f, "MoveSpeed", 0f, 2.5f);

            // 4. 10% phun ra quả cầu lửa: 300 damage
            CreateDamageProc("CauLua_300Dmg", 10f, 300f, 0f);

            // 5. 15% kích hoạt Lô Sắc gây sát thương lên kẻ địch: 400 damage
            CreateDamageProc("LoSac_400Dmg", 15f, 400f, 0f);

            // 6. Mỗi lần giết 1 kẻ thù, có 50% xuất hiện goblin đánh bom kẻ thù: 300 damage
            // (Note: Spawning logic requires a prefab, we create the config, user will drag the Goblin prefab later)
            CreateSpawnProc("GoblinBomber_OnKill", 50f, EProcTriggerCondition.OnKill);

            // 7. Tăng tốc đánh của đồng minh trong phạm vi 700
            CreateBuffProc("BuffAtkSpeed_Range700", 100f, EProcTriggerCondition.OnAttackStart, "AttackCooldown", 0.8f, 700f, 5f);

            // 8. Khi tấn công, 10% phát ra độc tố gây sát thương 125
            CreateDamageProc("DocTo_125Dmg", 10f, 125f, 0f);

            // 9. 3% kích hoạt 5 lần sát thương
            // (Simulated as just a big chunk for now, or a DoT effect 5 ticks)
            CreateDoTProc("MultiStrike_5Times", 3f, 5f, 5, "Health");

            // 10. Khi tấn công có 30% gây bất động kẻ thù, damage 275
            CreateCCProc("BatDong_275Dmg", 30f, 275f, "MoveSpeed", 0f, 2f);

            // 11. 5% tạo 1 vụ nổ nhỏ, làm chậm kẻ địch, damage 200, kéo dài 4s
            CreateCCProc("NoNhoSlow_200Dmg", 5f, 200f, "MoveSpeed", 0.6f, 4f, 200f);

            // 12. 10% tạo trận bão tuyết, tấn công 5 lần, sát thương 175
            CreateDoTProc("BaoTuyet_175Dmg", 10f, 175f, 5, "Health", 400f);

            // 13. Triệu hồi sâu băng có 50% giết chết kẻ thù, sát thương 250
            CreateSpawnProc("SauBang_Kill", 50f, EProcTriggerCondition.OnKill);

            // 14. 10% kích hoạt thiên tướng, tăng gấp đôi tốc đánh
            CreateBuffProc("ThienTuong_DoubleAtk", 10f, EProcTriggerCondition.OnAttackStart, "AttackCooldown", 0.5f, 0f, 5f, EProcContextTarget.Source);

            // 15. Khi tấn công, 10% nhốt kẻ thù vào lồng băng, sát thương 600, 2s
            CreateCCProc("LongBang_600Dmg", 10f, 600f, "MoveSpeed", 0f, 2f);

            // 16. 20% kêu gọi sấm thiên lôi. Sát thương 250, choáng 0.5
            CreateCCProc("ThienLoi_250Dmg", 20f, 250f, "MoveSpeed", 0f, 0.5f);

            // 17. Tấn công 3 enemies cùng lúc
            // (This modifies the base ability directly usually, but for a proc we can just say AoE damage 3 enemies)
            CreateDamageProc("Attack_3_Enemies", 100f, 100f, 300f);

            // 18. Khi tấn công 10% hút sinh lực phạm vi 700, sát thương 450
            CreateDamageProc("HutSinhLuc_450Dmg", 10f, 450f, 700f);

            // 19. 10% gọi Mộc Thần hỗ trợ, sát thương 450
            CreateDamageProc("MocThan_450Dmg", 10f, 450f, 0f);

            // 20. 5% kích hoạt núi vàng, sát thương 400, tấn công 10 lần trong 10s
            CreateDoTProc("NuiVang_400Dmg", 5f, 400f, 10, "Health", 500f);

            // 21. 5% kích hoạt nổ mạnh, gây sát thương 3000
            CreateDamageProc("NoManh_3000Dmg", 5f, 3000f, 400f);

            // 22. 10% đám cháy, 225 dmg/s, 5s
            CreateDoTProc("DamChay_225Dmg", 10f, 225f, 5, "Health", 300f);

            // 23. 10% thổi khí độc làm mờ mắt, giảm tốc chạy 40%, phạm vi 275
            CreateCCProc("KhiDoc_Slow40", 10f, 0f, "MoveSpeed", 0.6f, 3f, 275f);

            // 24. 5% hạt nhân 1500 damage
            CreateDamageProc("BomHatNhan_1500Dmg", 5f, 1500f, 600f);

            // 25. 15% mũi tên 300 damage
            CreateSpawnProc("MuiTen_300Dmg", 15f, EProcTriggerCondition.OnHit);

            // 26. 10% mẹ thiên nhiên, sát thương 200, trói 2s
            CreateCCProc("MeThienNhien_200Dmg", 10f, 200f, "MoveSpeed", 0f, 2f);

            // 27. 10% yểm chú kéo 100 dmg, 7s
            CreateDoTProc("YemChu_100Dmg", 10f, 100f, 7, "Health");

            // 28. 10% cơn sóng 500 damage
            CreateDamageProc("ConSong_500Dmg", 10f, 500f, 400f);

            // 29. 20% động đất, 300 dmg, chậm 0.5s
            CreateCCProc("DongDat_300Dmg", 20f, 300f, "MoveSpeed", 0.6f, 0.5f, 500f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Successfully generated all bulky proc abilities in the Data folder!");
        }

        // --- THE GRANULAR MODULAR GENERATOR FUNCTIONS ---

        public static void CreateDamageProc(string name, float chance, float damage, float aoeRadius = 0f)
        {
            var procData = ScriptableObject.CreateInstance<TD_BaseProcData>();
            procData.abilityName = name;
            procData.abilityID = name;
            procData.chance = chance;
            procData.flatDamage = damage;
            procData.aoeRadius = aoeRadius;
            procData.triggerType = EProcTriggerCondition.OnHit;

            SaveProcAsset(procData, $"Proc_DMG_{name}");
        }

        public static void CreateCCProc(string name, float chance, float damage, string ccAttribute, float magnitude, float duration, float aoeRadius = 0f)
        {
            var effect = CreateGameplayEffect($"GE_CC_{name}", ccAttribute, magnitude, duration, EGameplayModifierOp.Multiply);

            var procData = ScriptableObject.CreateInstance<TD_BaseProcData>();
            procData.abilityName = name;
            procData.abilityID = name;
            procData.chance = chance;
            procData.flatDamage = damage;
            procData.effectToApply = effect;
            procData.aoeRadius = aoeRadius;
            procData.triggerType = EProcTriggerCondition.OnHit;

            SaveProcAsset(procData, $"Proc_CC_{name}");
        }

        public static void CreateDoTProc(string name, float chance, float damagePerTick, int ticks, string attribute, float aoeRadius = 0f)
        {
            var effect = CreateGameplayEffect($"GE_DoT_{name}", attribute, -damagePerTick, ticks, EGameplayModifierOp.Add);
            effect.isPeriodic = true;
            effect.period = 1.0f; // 1 tick per second

            var procData = ScriptableObject.CreateInstance<TD_BaseProcData>();
            procData.abilityName = name;
            procData.abilityID = name;
            procData.chance = chance;
            procData.effectToApply = effect;
            procData.aoeRadius = aoeRadius;
            procData.triggerType = EProcTriggerCondition.OnHit;

            SaveProcAsset(procData, $"Proc_DoT_{name}");
        }

        public static void CreateBuffProc(string name, float chance, EProcTriggerCondition trigger, string statToBuff, float magnitude, float aoeRadius = 0f, float duration = 5f, EProcContextTarget target = EProcContextTarget.Target)
        {
            var effect = CreateGameplayEffect($"GE_Buff_{name}", statToBuff, magnitude, duration, EGameplayModifierOp.Multiply);

            var procData = ScriptableObject.CreateInstance<TD_BaseProcData>();
            procData.abilityName = name;
            procData.abilityID = name;
            procData.chance = chance;
            procData.triggerType = trigger;
            procData.effectToApply = effect;
            procData.aoeRadius = aoeRadius;
            procData.executionTarget = target;

            SaveProcAsset(procData, $"Proc_Buff_{name}");
        }

        public static void CreateSpawnProc(string name, float chance, EProcTriggerCondition trigger)
        {
            var procData = ScriptableObject.CreateInstance<TD_BaseProcData>();
            procData.abilityName = name;
            procData.abilityID = name;
            procData.chance = chance;
            procData.triggerType = trigger;

            // Cannot assign prefab via script cleanly without specific paths, so we leave it empty for the designer to drag and drop.
            SaveProcAsset(procData, $"Proc_Spawn_{name}");
        }

        // --- INTERNAL HELPERS ---

        private static GameplayEffect CreateGameplayEffect(string assetName, string attributeName, float magnitude, float duration, EGameplayModifierOp op)
        {
            string effectPath = $"{FOLDER_PATH}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GameplayEffect>(effectPath);
            if (existing != null) return existing;

            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.durationType = duration > 0 ? EGameplayEffectDurationType.Duration : EGameplayEffectDurationType.Instant;
            effect.durationMagnitude = duration;

            System.Enum.TryParse(attributeName, out EGameplayAttributeType attrType);

            var modifier = new GameplayEffectModifier(attrType, op, magnitude);
            effect.modifiers = new[] { modifier };

            AssetDatabase.CreateAsset(effect, effectPath);
            return effect;
        }

        private static void SaveProcAsset(TD_BaseProcData data, string assetName)
        {
            string path = $"{FOLDER_PATH}/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<TD_BaseProcData>(path) == null)
            {
                AssetDatabase.CreateAsset(data, path);
            }
        }

        private static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Master/TranHuongDao/Core/Abilities"))
                AssetDatabase.CreateFolder("Assets/_Master/TranHuongDao/Core", "Abilities");

            if (!AssetDatabase.IsValidFolder(FOLDER_PATH))
                AssetDatabase.CreateFolder("Assets/_Master/TranHuongDao/Core/Abilities", "Data");
        }
    }
}
#endif
