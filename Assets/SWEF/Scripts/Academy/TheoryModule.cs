// TheoryModule.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>A single multiple-choice or true/false question in a theory quiz.</summary>
    [Serializable]
    public class TheoryQuestion
    {
        /// <summary>The question text shown to the player.</summary>
        [TextArea(1, 4)]
        public string questionText;

        /// <summary>Answer choices (at least 2, at most 4).</summary>
        public List<string> choices = new List<string>();

        /// <summary>Zero-based index of the correct answer in <see cref="choices"/>.</summary>
        [Range(0, 3)]
        public int correctAnswerIndex;

        /// <summary>
        /// Explanation shown after the player answers — why the correct answer is right.
        /// </summary>
        [TextArea(1, 4)]
        public string explanation;
    }

    /// <summary>
    /// Theory component of a <see cref="FlightLesson"/>: a set of questions and a
    /// minimum passing score.
    /// </summary>
    [Serializable]
    public class TheoryModule
    {
        /// <summary>Display title for this quiz section.</summary>
        public string moduleTitle;

        /// <summary>Brief overview text shown before the quiz begins.</summary>
        [TextArea(2, 5)]
        public string introductionText;

        /// <summary>Ordered list of questions.</summary>
        public List<TheoryQuestion> questions = new List<TheoryQuestion>();

        /// <summary>Minimum percentage score (0–100) required to pass.</summary>
        [Range(0f, 100f)]
        public float passingScore = 70f;

        // ── Scoring helper ─────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates a list of player answer indices and returns the percentage score.
        /// </summary>
        /// <param name="playerAnswers">
        /// Zero-based indices of the player's chosen answer for each question, in order.
        /// </param>
        /// <returns>Score as a percentage (0–100), or 0 if there are no questions.</returns>
        public float CalculateScore(IList<int> playerAnswers)
        {
            if (questions == null || questions.Count == 0) return 0f;
            if (playerAnswers == null || playerAnswers.Count == 0) return 0f;

            int correct = 0;
            int count   = Mathf.Min(questions.Count, playerAnswers.Count);
            for (int i = 0; i < count; i++)
            {
                if (playerAnswers[i] == questions[i].correctAnswerIndex)
                    correct++;
            }
            return (float)correct / questions.Count * 100f;
        }

        /// <summary>Returns <c>true</c> if the given score meets the passing threshold.</summary>
        public bool IsPassing(float score) => score >= passingScore;

        public override string ToString() =>
            $"[Theory:{moduleTitle}] {questions?.Count ?? 0} questions | Pass: {passingScore}%";
    }
}
