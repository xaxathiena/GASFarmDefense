using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Abel.TranHuongDao.Core;

namespace Abel.TranHuongDao.EditorTools
{
    [CustomEditor(typeof(TowerBuilderConfigSO))]
    public class TowerBuilderConfigEditor : UnityEditor.Editor
    {
        private static readonly string[] DefaultTowerIDs = new string[]
        {
            "unit_frog_tower", "unit_grunt_tower", "unit_horse_tower", "unit_murlock_tower",
            "unit_golin_tower", "unit_scout_tower", "unit_wisdom_tower", "unit_turle_tower",
            "unit_radar_tower", "unit_chicken_tower", "unit_siren_tower", "unit_goblin_saipa_tower",
            "unit_guard_tower", "unit_dryad_tower", "unit_goblin_zepplin_tower", "unit_crab_tower",
            "unit_windrider_tower", "unit_flying_sheep_tower", "unit_royal_guard_tower", "unit_canon_tower",
            "unit_huntress_tower", "unit_owl_tower", "unit_demolisher_tower", "unit_cowl_tower",
            "unit_hydra_tower", "unit_goblin_blaster_tower", "unit_obelisk_tower", "unit_mountain_giant_tower",
            "unit_sentiel_tower", "unit_oak_jugunet_tower", "unit_pig_tower", "unit_coreta_tower",
            "unit_goblin_factory_tower", "unit_magic_castle_tower", "unit_chimera_tower", "unit_stone_tower",
            "unit_daemon_tower", "unit_dimension_tower", "unit_ice_crown_tower", "unit_black_citadel_tower",
            "unit_magic_castle_tower", "unit_azshara_tower", "unit_fascia_tower", "unit_doom_guard_tower",
            "unit_dread_lord_tower", "unit_malfurion_tower", "unit_alchemist_tower", "unit_prison_wagon_tower",
            "unit_infernal_tower", "unit_holy_azhzra_tower", "unit_hell_fire_tower", "unit_anthony_das_tower",
            "unit_world_tree_tower", "unit_avatar_benece_tower", "unit_arthas_tower", "unit_mountain_king_tower"
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TowerBuilderConfigSO so = (TowerBuilderConfigSO)target;

            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("Populate All Tower IDs", GUILayout.Height(40)))
            {
                Undo.RecordObject(so, "Populate Tower IDs");
                
                if (so.config == null) so.config = new TowerBuilderConfig();
                if (so.config.availableTowerIDs == null) so.config.availableTowerIDs = new List<string>();

                so.config.availableTowerIDs.Clear();
                foreach (var id in DefaultTowerIDs)
                {
                    if (!so.config.availableTowerIDs.Contains(id))
                    {
                        so.config.availableTowerIDs.Add(id);
                    }
                }

                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssets();
                Debug.Log($"[TowerBuilderConfig] Added {DefaultTowerIDs.Length} tower IDs to {so.name}");
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
