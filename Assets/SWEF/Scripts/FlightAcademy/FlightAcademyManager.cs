using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Central singleton that orchestrates the Flight Training Academy.
    /// Loads <see cref="TrainingModule"/> assets from Resources/Academy/ (falling back
    /// to <see cref="FlightAcademyDefaultData"/> when none are found), manages license
    /// progression, and persists state to <c>academy_progress.json</c>.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class FlightAcademyManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static FlightAcademyManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a training module session begins.</summary>
        public event Action<TrainingModule> OnModuleStarted;

        /// <summary>Fired when an exam session begins.</summary>
        public event Action<TrainingModule> OnExamStarted;

        /// <summary>Fired when an exam is completed (pass or fail).</summary>
        public event Action<TrainingModule, ExamResult> OnExamCompleted;

        /// <summary>Fired when the player earns a new license grade.</summary>
        public event Action<LicenseGrade> OnLicenseEarned;

        /// <summary>Fired when a new certificate is issued.</summary>
        public event Action<Certificate> OnCertificateIssued;

        // ── State ─────────────────────────────────────────────────────────────────
        private TrainingModule[] _allModules;
        private AcademyProgress _progress;
        private TrainingModule _activeModule;
        private bool _examActive;

        private const string SaveFileName = "academy_progress.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Unity ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadModules();
            LoadProgress();
        }

        // ── Module Loading ─────────────────────────────────────────────────────────
        private void LoadModules()
        {
            _allModules = Resources.LoadAll<TrainingModule>("Academy");
            if (_allModules == null || _allModules.Length == 0)
                _allModules = FlightAcademyDefaultData.CreateDefaultModules();
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Returns all modules belonging to the given <paramref name="grade"/>.</summary>
        public List<TrainingModule> GetAvailableModules(LicenseGrade grade)
        {
            var result = new List<TrainingModule>();
            if (_allModules == null) return result;
            foreach (var m in _allModules)
            {
                if (m.licenseGrade == grade)
                    result.Add(m);
            }
            return result;
        }

        /// <summary>Returns all modules available to the player given current progress.</summary>
        public List<TrainingModule> GetUnlockedModules()
        {
            var unlocked = new List<TrainingModule>();
            if (_allModules == null) return unlocked;
            foreach (var m in _allModules)
            {
                if (IsModuleUnlocked(m))
                    unlocked.Add(m);
            }
            return unlocked;
        }

        /// <summary>Returns true if all prerequisites are satisfied for <paramref name="module"/>.</summary>
        public bool IsModuleUnlocked(TrainingModule module)
        {
            if (module.prerequisiteModuleIds == null || module.prerequisiteModuleIds.Count == 0)
                return true;
            foreach (var prereq in module.prerequisiteModuleIds)
            {
                if (!_progress.completedModules.Contains(prereq))
                    return false;
            }
            return true;
        }

        /// <summary>Begins a guided training session for <paramref name="module"/>.</summary>
        public void StartModule(TrainingModule module)
        {
            if (module == null) return;
            _activeModule = module;
            OnModuleStarted?.Invoke(module);
        }

        /// <summary>Begins the practical exam for <paramref name="module"/>.</summary>
        public void StartExam(TrainingModule module)
        {
            if (module == null || _examActive) return;
            _activeModule = module;
            _examActive = true;
            OnExamStarted?.Invoke(module);
        }

        /// <summary>
        /// Records the result of the active exam.
        /// Handles best-score tracking, module completion, and license progression.
        /// </summary>
        public void CompleteExam(ExamResult result)
        {
            if (_activeModule == null || result == null) return;
            _examActive = false;

            string moduleId = _activeModule.moduleId;

            // Keep best score
            bool improved = false;
            if (!_progress.examResults.ContainsKey(moduleId)
                || result.score > _progress.examResults[moduleId].score)
            {
                _progress.examResults[moduleId] = result;
                improved = true;
            }

            OnExamCompleted?.Invoke(_activeModule, result);

            if (result.passed && !_progress.completedModules.Contains(moduleId))
            {
                _progress.completedModules.Add(moduleId);
                CheckLicenseProgression(_activeModule.licenseGrade);
            }

            _activeModule = null;
            SaveProgress();
        }

        /// <summary>Returns the player's current highest license grade.</summary>
        public LicenseGrade GetCurrentLicense() => _progress.currentLicense;

        /// <summary>Returns a copy of the full academy progress state.</summary>
        public AcademyProgress GetAcademyProgress() => _progress;

        // ── License Progression ───────────────────────────────────────────────────
        private void CheckLicenseProgression(LicenseGrade grade)
        {
            if (grade <= _progress.currentLicense) return;

            var modulesInGrade = GetAvailableModules(grade);
            foreach (var m in modulesInGrade)
            {
                if (!_progress.completedModules.Contains(m.moduleId))
                    return;
            }

            // All modules passed — award license
            _progress.currentLicense = grade;
            OnLicenseEarned?.Invoke(grade);

            var cert = CertificateGenerator.GenerateCertificate(
                grade,
                GetPlayerName(),
                BuildExamScoreDict(grade),
                _progress.totalTrainingHours);

            _progress.certificates.Add(cert);
            OnCertificateIssued?.Invoke(cert);
            SaveProgress();
        }

        private string GetPlayerName()
        {
#if UNITY_EDITOR
            return "Pilot";
#else
            return Application.identifier;
#endif
        }

        private Dictionary<string, float> BuildExamScoreDict(LicenseGrade grade)
        {
            var dict = new Dictionary<string, float>();
            foreach (var m in GetAvailableModules(grade))
            {
                if (_progress.examResults.TryGetValue(m.moduleId, out var r))
                    dict[m.moduleId] = r.score;
            }
            return dict;
        }

        // ── Training Hours ────────────────────────────────────────────────────────
        /// <summary>Adds flight hours to the total training hours counter.</summary>
        public void AddTrainingHours(float hours)
        {
            if (hours <= 0f) return;
            _progress.totalTrainingHours += hours;
            SaveProgress();
        }

        // ── Persistence ───────────────────────────────────────────────────────────
        private void LoadProgress()
        {
            if (!File.Exists(SavePath))
            {
                _progress = new AcademyProgress();
                return;
            }
            try
            {
                string json = File.ReadAllText(SavePath);
                var saveData = JsonUtility.FromJson<AcademyProgressSaveData>(json);
                _progress = ConvertFromSaveData(saveData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FlightAcademyManager] Failed to load progress: {e.Message}");
                _progress = new AcademyProgress();
            }
        }

        private void SaveProgress()
        {
            try
            {
                var saveData = ConvertToSaveData(_progress);
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FlightAcademyManager] Failed to save progress: {e.Message}");
            }
        }

        private static AcademyProgressSaveData ConvertToSaveData(AcademyProgress p)
        {
            var save = new AcademyProgressSaveData
            {
                currentLicense = p.currentLicense,
                completedModules = new List<string>(p.completedModules),
                totalTrainingHours = p.totalTrainingHours
            };
            foreach (var kvp in p.examResults)
                save.examResults.Add(new ExamResultEntry { moduleId = kvp.Key, result = kvp.Value });
            foreach (var cert in p.certificates)
            {
                var certSave = new CertificateSaveData
                {
                    certificateId = cert.certificateId,
                    licenseGrade = cert.licenseGrade,
                    playerName = cert.playerName,
                    issueDate = cert.issueDate,
                    totalFlightHours = cert.totalFlightHours,
                    signatureHash = cert.signatureHash
                };
                foreach (var kvp in cert.examScores)
                    certSave.examScores.Add(new CertificateExamScoreEntry { moduleId = kvp.Key, score = kvp.Value });
                save.certificates.Add(certSave);
            }
            return save;
        }

        private static AcademyProgress ConvertFromSaveData(AcademyProgressSaveData save)
        {
            var p = new AcademyProgress
            {
                currentLicense = save.currentLicense,
                completedModules = new List<string>(save.completedModules),
                totalTrainingHours = save.totalTrainingHours
            };
            foreach (var entry in save.examResults)
                p.examResults[entry.moduleId] = entry.result;
            foreach (var certSave in save.certificates)
            {
                var cert = new Certificate
                {
                    certificateId = certSave.certificateId,
                    licenseGrade = certSave.licenseGrade,
                    playerName = certSave.playerName,
                    issueDate = certSave.issueDate,
                    totalFlightHours = certSave.totalFlightHours,
                    signatureHash = certSave.signatureHash
                };
                foreach (var e in certSave.examScores)
                    cert.examScores[e.moduleId] = e.score;
                p.certificates.Add(cert);
            }
            return p;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
