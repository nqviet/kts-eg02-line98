using System;
using System.Collections.Generic;
using Line98.Data;
using UnityEngine;

namespace Line98.Services
{
    public enum SettingsField : byte
    {
        Music,
        Sfx,
        Vibration,
        ReduceEffects,
        PatternHints,
        Locale
    }

    public readonly struct SettingsChanged
    {
        public readonly SettingsField Field;
        public readonly bool BoolValue;
        public readonly string StringValue;

        public SettingsChanged(SettingsField field, bool boolValue, string stringValue = null)
        {
            Field = field;
            BoolValue = boolValue;
            StringValue = stringValue;
        }
    }

    public readonly struct LocaleRow
    {
        public readonly string Code;
        public readonly string DisplayName;
        public readonly string NativeName;

        public LocaleRow(string code, string displayName, string nativeName)
        {
            Code = code;
            DisplayName = displayName;
            NativeName = nativeName;
        }
    }

    public readonly struct SettingsReadModel
    {
        public readonly bool Music;
        public readonly bool Sfx;
        public readonly bool Vibration;
        public readonly bool ReduceEffects;
        public readonly LocaleRow CurrentLocale;
        public readonly LocaleRow[] AvailableLocales;
        public readonly PurchaseState RemoveAdsState;
        public readonly bool CanRestorePurchases;
        public readonly string VersionLabel;
        public readonly string PrivacyUrl;
        public readonly string SupportUrl;
        public readonly bool PatternHints;

        public SettingsReadModel(
            bool music,
            bool sfx,
            bool vibration,
            bool reduceEffects,
            LocaleRow currentLocale,
            LocaleRow[] availableLocales,
            PurchaseState removeAdsState,
            bool canRestorePurchases,
            string versionLabel,
            string privacyUrl,
            string supportUrl,
            bool patternHints)
        {
            Music = music;
            Sfx = sfx;
            Vibration = vibration;
            ReduceEffects = reduceEffects;
            CurrentLocale = currentLocale;
            AvailableLocales = availableLocales ?? Array.Empty<LocaleRow>();
            RemoveAdsState = removeAdsState;
            CanRestorePurchases = canRestorePurchases;
            VersionLabel = versionLabel;
            PrivacyUrl = privacyUrl;
            SupportUrl = supportUrl;
            PatternHints = patternHints;
        }
    }

    public interface ISettingsService
    {
        SettingsSave Current { get; }
        event Action<SettingsChanged> OnChanged;

        SettingsReadModel BuildReadModel(IIapService purchases, IBrandConfig brand);

        void Load(SettingsSave data);
        SettingsSave Snapshot();

        void SetMusic(bool enabled);
        void SetSfx(bool enabled);
        void SetVibration(bool enabled);
        void SetReduceEffects(bool enabled);
        void SetPatternHints(bool enabled);
        void SetLocale(string localeCode);

        IReadOnlyList<LocaleRow> AvailableLocales { get; }
    }

    public sealed class SettingsService : ISettingsService
    {
        private const string LegacyVibrationKey = "line98_pref_vibration";
        private const string LegacyReduceEffectsKey = "line98_pref_reduce_fx";

        private static readonly LocaleRow[] s_AvailableLocales = new[]
        {
            new LocaleRow("en", "ENGLISH", "English"),
            new LocaleRow("vi", "TIẾNG VIỆT", "Tiếng Việt")
        };

        private readonly SettingsSave m_Settings;

        public SettingsSave Current => m_Settings;
        public event Action<SettingsChanged> OnChanged;
        public IReadOnlyList<LocaleRow> AvailableLocales => s_AvailableLocales;

        public SettingsService(SettingsSave initial = null)
        {
            m_Settings = initial ?? new SettingsSave();
            MigrateLegacyPlayerPrefs();
        }

        public void Load(SettingsSave data)
        {
            if (data == null) return;
            m_Settings.MusicEnabled = data.MusicEnabled;
            m_Settings.SfxEnabled = data.SfxEnabled;
            m_Settings.VibrationEnabled = data.VibrationEnabled;
            m_Settings.ReduceEffects = data.ReduceEffects;
            m_Settings.PatternHints = data.PatternHints;
            m_Settings.LocaleCode = !string.IsNullOrEmpty(data.LocaleCode) ? data.LocaleCode : "en";
        }

        public SettingsSave Snapshot()
        {
            return new SettingsSave
            {
                MusicEnabled = m_Settings.MusicEnabled,
                SfxEnabled = m_Settings.SfxEnabled,
                VibrationEnabled = m_Settings.VibrationEnabled,
                ReduceEffects = m_Settings.ReduceEffects,
                PatternHints = m_Settings.PatternHints,
                LocaleCode = m_Settings.LocaleCode
            };
        }

        public void SetMusic(bool enabled)
        {
            if (m_Settings.MusicEnabled == enabled) return;
            m_Settings.MusicEnabled = enabled;
            OnChanged?.Invoke(new SettingsChanged(SettingsField.Music, enabled));
        }

        public void SetSfx(bool enabled)
        {
            if (m_Settings.SfxEnabled == enabled) return;
            m_Settings.SfxEnabled = enabled;
            OnChanged?.Invoke(new SettingsChanged(SettingsField.Sfx, enabled));
        }

        public void SetVibration(bool enabled)
        {
            if (m_Settings.VibrationEnabled == enabled) return;
            m_Settings.VibrationEnabled = enabled;
            PlayerPrefs.SetInt(LegacyVibrationKey, enabled ? 1 : 0);
            OnChanged?.Invoke(new SettingsChanged(SettingsField.Vibration, enabled));
        }

        public void SetReduceEffects(bool enabled)
        {
            if (m_Settings.ReduceEffects == enabled) return;
            m_Settings.ReduceEffects = enabled;
            PlayerPrefs.SetInt(LegacyReduceEffectsKey, enabled ? 1 : 0);
            OnChanged?.Invoke(new SettingsChanged(SettingsField.ReduceEffects, enabled));
        }

        public void SetPatternHints(bool enabled)
        {
            if (m_Settings.PatternHints == enabled) return;
            m_Settings.PatternHints = enabled;
            OnChanged?.Invoke(new SettingsChanged(SettingsField.PatternHints, enabled));
        }

        public void SetLocale(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode) || m_Settings.LocaleCode == localeCode) return;
            m_Settings.LocaleCode = localeCode;
            OnChanged?.Invoke(new SettingsChanged(SettingsField.Locale, false, localeCode));
        }

        public SettingsReadModel BuildReadModel(IIapService purchases, IBrandConfig brand)
        {
            LocaleRow currentLocale = s_AvailableLocales[0];
            for (int i = 0; i < s_AvailableLocales.Length; i++)
            {
                if (string.Equals(s_AvailableLocales[i].Code, m_Settings.LocaleCode, StringComparison.OrdinalIgnoreCase))
                {
                    currentLocale = s_AvailableLocales[i];
                    break;
                }
            }

            PurchaseState purchaseState = purchases != null ? purchases.RemoveAdsState : PurchaseState.NotOwned;
            bool canRestore = purchases != null && purchases.CanRestorePurchases;
            string version = brand != null ? brand.VersionLabel : Application.version;
            string privacy = brand != null ? brand.PrivacyUrl : null;
            string support = brand != null ? brand.SupportUrl : null;

            return new SettingsReadModel(
                m_Settings.MusicEnabled,
                m_Settings.SfxEnabled,
                m_Settings.VibrationEnabled,
                m_Settings.ReduceEffects,
                currentLocale,
                s_AvailableLocales,
                purchaseState,
                canRestore,
                version,
                privacy,
                support,
                m_Settings.PatternHints);
        }

        private void MigrateLegacyPlayerPrefs()
        {
            if (PlayerPrefs.HasKey(LegacyVibrationKey))
            {
                m_Settings.VibrationEnabled = PlayerPrefs.GetInt(LegacyVibrationKey, 1) == 1;
            }
            if (PlayerPrefs.HasKey(LegacyReduceEffectsKey))
            {
                m_Settings.ReduceEffects = PlayerPrefs.GetInt(LegacyReduceEffectsKey, 0) == 1;
            }
        }
    }
}
