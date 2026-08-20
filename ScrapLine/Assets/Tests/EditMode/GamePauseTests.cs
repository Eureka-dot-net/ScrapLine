using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ScrapLine.Tests.EditMode
{
    public sealed class GamePauseTests
    {
        private Type gameManagerType;
        private GameObject gameManagerObject;
        private Component gameManager;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            gameManagerType = Type.GetType("GameManager, Assembly-CSharp", true);
            SetSingletonInstance(null);
            gameManagerObject = new GameObject("GameManagerPauseTest");
            gameManager = gameManagerObject.AddComponent(gameManagerType);
        }

        [TearDown]
        public void TearDown()
        {
            if (gameManagerObject != null)
                UnityEngine.Object.DestroyImmediate(gameManagerObject);

            SetSingletonInstance(null);
            Time.timeScale = 1f;
        }

        [Test]
        public void PauseLeavesUnityClockRunningForRenderingAndUi()
        {
            Invoke("SetSimulationPaused", true);

            Assert.That(GetPausedState(), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Invoke("SetSimulationPaused", false);

            Assert.That(GetPausedState(), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void TogglePauseAlternatesState()
        {
            Invoke("ToggleSimulationPause");
            Assert.That(GetPausedState(), Is.True);

            Invoke("ToggleSimulationPause");
            Assert.That(GetPausedState(), Is.False);
        }

        private bool GetPausedState()
        {
            return (bool)gameManagerType.GetProperty("IsSimulationPaused").GetValue(gameManager);
        }

        private void Invoke(string methodName, params object[] arguments)
        {
            gameManagerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
                .Invoke(gameManager, arguments);
        }

        private void SetSingletonInstance(object value)
        {
            gameManagerType.GetField("<Instance>k__BackingField",
                    BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, value);
        }
    }
}
