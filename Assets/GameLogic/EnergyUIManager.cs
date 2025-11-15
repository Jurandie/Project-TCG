using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GameLogic
{
    public class EnergyUIManager : MonoBehaviour
    {
        [Header("Referências")]
        public EnergyManager energyManager;
        public Transform playerEnergyContainer;
        public Transform enemyEnergyContainer;
        public GameObject manaGemPrefab;

        [Header("Sprites")]
        public Sprite fullGem;
        public Sprite emptyGem;

        [Header("Animação e Estilo")]
        public float pulseScale = 1.2f;
        public float pulseSpeed = 4f;

        private readonly List<Image> playerGems = new List<Image>();
        private readonly List<Image> enemyGems = new List<Image>();

        void Awake()
        {
            if (energyManager != null)
                energyManager.OnEnergyChanged += HandleEnergyChanged;
        }

        void Start()
        {
            if (energyManager == null)
            {
                Debug.LogError("[EnergyUIManager] EnergyManager não atribuído!");
                return;
            }

            GenerateGems();
            UpdateEnergyDisplay(energyManager.playerEnergy, energyManager.enemyEnergy);
        }

        void OnDestroy()
        {
            if (energyManager != null)
                energyManager.OnEnergyChanged -= HandleEnergyChanged;
        }

        void Update()
        {
            AnimateActiveGems();
        }

        void HandleEnergyChanged(int player, int enemy)
        {
            UpdateEnergyDisplay(player, enemy);
        }

        void GenerateGems()
        {
            foreach (Transform child in playerEnergyContainer) Destroy(child.gameObject);
            foreach (Transform child in enemyEnergyContainer) Destroy(child.gameObject);

            playerGems.Clear();
            enemyGems.Clear();

            for (int i = 0; i < energyManager.maxEnergy; i++)
            {
                var pGem = Instantiate(manaGemPrefab, playerEnergyContainer);
                var eGem = Instantiate(manaGemPrefab, enemyEnergyContainer);

                var pImg = pGem.GetComponent<Image>();
                var eImg = eGem.GetComponent<Image>();

                pImg.sprite = emptyGem;
                eImg.sprite = emptyGem;

                playerGems.Add(pImg);
                enemyGems.Add(eImg);
            }
        }

        public void UpdateEnergyDisplay(int playerEnergy, int enemyEnergy)
        {
            for (int i = 0; i < energyManager.maxEnergy; i++)
            {
                playerGems[i].sprite = i < playerEnergy ? fullGem : emptyGem;
                enemyGems[i].sprite = i < enemyEnergy ? fullGem : emptyGem;
            }
        }

        void AnimateActiveGems()
        {
            float baseScale = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.05f;

            foreach (var img in playerGems)
                img.transform.localScale = img.sprite == fullGem ? Vector3.one * (baseScale * pulseScale) : Vector3.one;

            foreach (var img in enemyGems)
                img.transform.localScale = img.sprite == fullGem ? Vector3.one * (baseScale * pulseScale) : Vector3.one;
        }
    }
}
