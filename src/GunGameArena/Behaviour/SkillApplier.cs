using System;
using System.Reflection;
using FistVR;
using GunGameArena.Core;
using GunGameArena.Patches;
using HarmonyLib;
using UnityEngine;

namespace GunGameArena.Behaviour
{
    /// <summary>Remembers a weapon's original values so re-applying a tier is idempotent.</summary>
    public class TierAppliedMarker : MonoBehaviour
    {
        public float Spread, FireAngle;
        public Vector2 Refire;
    }

    /// <summary>Applies skill-tier multipliers (spec §4b.4). Rookies miss more; nobody fires more.</summary>
    public static class SkillApplier
    {
        private static readonly FieldInfo RecognitionField = AccessTools.Field(typeof(Sosig), "m_entityRecognitionMultiplier");
        private static readonly FieldInfo IdentificationField = AccessTools.Field(typeof(Sosig), "m_combatTargetIdentificationSpeedMultiplier");
        private static bool _warnedMissingFields;

        public static void Install()
        {
            SpawnerPatches.SosigBound += Apply;
        }

        public static void Apply(Slot slot)
        {
            try
            {
                if (!ArenaConfig.SkillTiers.Value || slot == null || slot.Sosig == null) return;
                Sosig s = slot.Sosig;
                TierMultipliers m = ArenaConfig.MultipliersFor(slot.Contestant.Tier);

                if (s.GetComponent<TierAppliedMarker>() == null)
                {
                    s.gameObject.AddComponent<TierAppliedMarker>();
                    ScaleFloatField(s, RecognitionField, m.Reaction);
                    ScaleFloatField(s, IdentificationField, m.Reaction);
                }
                for (int i = 0; i < s.Hands.Count; i++)
                {
                    if (s.Hands[i] != null && s.Hands[i].IsHoldingObject && s.Hands[i].HeldObject != null)
                        ApplyWeapon(s.Hands[i].HeldObject, slot.Contestant.Tier);
                }
                Plugin.Log.LogInfo("Tier " + slot.Contestant.Tier + " applied to " + slot.Contestant.Name);
            }
            catch (Exception e) { Plugin.Log.LogError("SkillApplier.Apply: " + e); }
        }

        public static void ApplyWeapon(SosigWeapon w, SkillTier tier)
        {
            if (w == null) return;
            TierMultipliers m = ArenaConfig.MultipliersFor(tier);
            var marker = w.GetComponent<TierAppliedMarker>();
            if (marker == null)
            {
                marker = w.gameObject.AddComponent<TierAppliedMarker>();
                marker.Spread = w.ProjectileSpread;
                marker.FireAngle = w.MaxAngularFireRange;
                marker.Refire = w.Usage_RefireRange;
            }
            w.ProjectileSpread = marker.Spread * m.Spread;
            w.MaxAngularFireRange = marker.FireAngle * m.FireAngle;
            w.Usage_RefireRange = marker.Refire * m.Refire;
        }

        private static void ScaleFloatField(Sosig s, FieldInfo field, float factor)
        {
            if (field == null)
            {
                if (!_warnedMissingFields) { Plugin.Log.LogWarning("Sosig reaction fields not found in this game build; tier reaction skipped."); _warnedMissingFields = true; }
                return;
            }
            float current = (float)field.GetValue(s);
            field.SetValue(s, current * factor);
        }
    }
}
