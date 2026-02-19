using System.Collections;
using UnityEngine;

namespace PGroup
{
    public class PipeController : MonoBehaviour
    {
        [SerializeField] private Transform scalingBaseWater;
        [SerializeField] private InteractableWater baseWater;
        [SerializeField] private Material baseWaterMat;
        [SerializeField] private Material dirtyWaterMat;
        [SerializeField] private Material greenWaterMat;

        private bool isWaterOn;
        private int currentWater;
        public void SetWater()
        {
            isWaterOn = !isWaterOn;
            transform.GetComponentInParent<PipeGroupPath>().SetupWaterFlow(isWaterOn);
        }
        public void StartWaterFlow(bool value)
        {
            isWaterOn = value;
            if (value)
            {
                baseWater.GenerateMesh();
                StopAllCoroutines();
                StartCoroutine(ScalingUpOvertime(scalingBaseWater.transform, new Vector3(1, 1, 1)));
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(ScalingUpOvertime(scalingBaseWater.transform, new Vector3(0, 1, 1)));
            }
        }
        public void SetColorWater(int num)
        {
            currentWater = num;
            switch (currentWater)
            {
                case 0:
                    baseWater.WaterMaterial = baseWaterMat;
                    break;
                case 1:
                    baseWater.WaterMaterial = dirtyWaterMat;
                    break;
                case 2:
                    baseWater.WaterMaterial = greenWaterMat;
                    break;
            }
            baseWater.GenerateMesh();
        }
        public void ChangeWater()
        {
            if(currentWater < 2)
            {
                currentWater++;

            }
            else
            {
                currentWater = 0;
            }
            transform.GetComponentInParent<PipeGroupPath>().ChangeAllWaterColor(currentWater);
        }
        public void ResetWater()
        {
            StopAllCoroutines();
            scalingBaseWater.transform.localScale = new Vector3(0, 1, 1);
        }

        private IEnumerator ScalingUpOvertime(Transform scaler, Vector3 targetScale)
        {
            Vector3 startScale = scaler.localScale;
            float time = 0f;

            while (time < 2)
            {
                float t = time / 2;
                scaler.localScale = Vector3.Lerp(startScale, targetScale, t);

                time += Time.deltaTime;
                yield return null;
            }
            scaler.localScale = targetScale;
        }
    }
}
