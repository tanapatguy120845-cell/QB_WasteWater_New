using System.Collections;
using UnityEngine;

namespace PGroup
{
    public class WaterLevelController : MonoBehaviour
    {
        [SerializeField] private InteractableWater baseWater;
        [SerializeField] private Transform baseWaterScale;
        [SerializeField] private InteractableWater dirtyWater;
        [SerializeField] private Transform dirtyWaterScale;
        [SerializeField] private InteractableWater greenWater;
        [SerializeField] private Transform greenWaterScale;

        private float currentBaseWater;
        private float currentDirtyWater;
        private float currentGreenWater;

        private TankData tankData;

        private void Start()
        {
            tankData = GetComponentInParent<TankData>();
        }
        public void SetBaseWater(float value)
        {
            baseWater.GenerateMesh();
            baseWaterScale.localScale = new Vector3(1, value, 1);
            currentBaseWater = value;
        }
        
        public void SetDirtyWater(float value)
        {
            dirtyWater.GenerateMesh();
            dirtyWaterScale.localScale = new Vector3(1, value, 1);
            currentDirtyWater = value;
        }
        
        public void SetGreenWater(float value)
        {
            greenWater.GenerateMesh();
            greenWaterScale.localScale = new Vector3(1, value, 1);
            currentGreenWater = value;
        }

        /*public void SetupBaseWater(bool isPlus)
        {
            SelectWaterType(true, isPlus);
        }
        public void SetupDirtyWater(bool isPlus)
        {
            SelectWaterType(false, isPlus);
        }*/
        public float GetBaseWaterLevel()
        {
            return currentBaseWater;
        }
        public float GetDirtyWaterLevel()
        {
            return currentDirtyWater;
        }
        public float GetGreenWaterLevel()
        {
            return currentGreenWater;
        }
        /*private void SelectWaterType(bool isBaseWater ,bool isPlus)
        {
            Transform selectedWaterScaling = null;
            InteractableWater selectedWater = null;
            int currentSelectedWater = 0;
            if (isBaseWater)
            {
                selectedWaterScaling = baseWaterScale;
                selectedWater = baseWater;
                currentSelectedWater = currentBaseWater;
                //Debug.Log($"SetBaseWater : {currentBaseWater}");
            }
            else
            {
                selectedWaterScaling = dirtyWaterScale;
                selectedWater = dirtyWater;
                currentSelectedWater = currentDirtyWater;
                //Debug.Log($"SetDirtyWater : {currentDirtyWater}");
            }
            if (isPlus)
            {
                currentSelectedWater++;
                switch (currentSelectedWater)
                {
                    case 1:
                        selectedWater.GenerateMesh();
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, .2f, 1)));
                        break;
                    case 2:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, .4f, 1)));
                        break;
                    case 3:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, .6f, 1)));
                        break;
                    case 4:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, .8f, 1)));
                        break;
                    case 5:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, 1f, 1)));
                        break;
                }
            }
            else
            {
                currentSelectedWater--;
                switch (currentSelectedWater)
                {
                    case 0:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, 0, 1)));
                        break;
                    case 1:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, .2f, 1)));
                        break;
                    case 2:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, .4f, 1)));
                        break;
                    case 3:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, .6f, 1)));
                        break;
                    case 4:
                        StopAllCoroutines();
                        StartCoroutine(ScalingUpOvertime(selectedWaterScaling.transform, new Vector3(1, .8f, 1)));
                        break;
                }
            }
            if (isBaseWater)
            {
                currentBaseWater = currentSelectedWater;
            }
            else
            {
                currentDirtyWater = currentSelectedWater;
            }

            if (tankData != null)
            {
                int getInt = 0;
                if(currentBaseWater > currentDirtyWater)
                {
                    getInt = currentBaseWater;
                }
                else
                {
                    getInt = currentDirtyWater;
                }

                tankData.UpdateWaterLevel(getInt);
            }
        }
        private IEnumerator ScalingUpOvertime(Transform scaler,Vector3 targetScale)
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
        }*/
    }
}
