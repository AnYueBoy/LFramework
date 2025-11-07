using System;
using UnityEngine;

namespace LFramework
{
    public class WaitForSeconds : YieldInstruction
    {
        private float timer;
        private readonly float interval;

        public WaitForSeconds(float seconds)
        {
            timer = 0;
            interval = seconds;
        }

        protected override bool IsCompleted()
        {
            timer += Time.deltaTime;
            return timer >= interval;
        }
    }
}