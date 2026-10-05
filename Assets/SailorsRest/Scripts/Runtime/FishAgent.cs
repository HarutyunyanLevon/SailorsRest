using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>A wild fish: a lazy patrol inside its depth band. Never reacts to the hook.</summary>
    public class FishAgent : MonoBehaviour
    {
        public const int Left = -1;
        public const int Right = 1;
        const float FullCircleRadians = Mathf.PI * 2f;

        public FishSpecies Species { get; private set; }
        public FishData Data { get; private set; }
        public bool Frozen { get; set; }
        public float IgnoreUntil { get; set; }

        GameBalance balance;
        SpriteRenderer body;
        float speed, minX, maxX, baseY, bobPhase;
        int facing;

        public void Init(GameBalance balance, FishSpecies species, FishData data, float minX, float maxX, float y)
        {
            this.balance = balance;
            Species = species;
            this.minX = minX;
            this.maxX = maxX;
            baseY = y;
            speed = Random.Range(balance.swimSpeed.x, balance.swimSpeed.y);
            facing = Random.Range(0, 2) == 0 ? Left : Right;
            bobPhase = Random.value * FullCircleRadians;
            body = gameObject.AddComponent<SpriteRenderer>();
            body.sortingOrder = SortOrder.Fish;
            body.sprite = species.sprite;
            SpriteFlipbook.Play(body, species.swimFrames, species.swimFps);   // SpriteCook swim loop, if downloaded
            SetData(data);
        }

        public void SetData(FishData data)
        {
            Data = data;
            body.color = Palette.Tint(data.Variant);
            float scale = balance.ScaleFor(data.Tier);
            transform.localScale = new Vector3(scale * facing, scale, 1f);
        }

        public void Face(int direction)
        {
            facing = direction < 0 ? Left : Right;
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            transform.localScale = scale;
        }

        void Update()
        {
            if (Frozen) return;
            var p = transform.position;
            p.x += facing * speed * Time.deltaTime;
            if (p.x < minX) { p.x = minX; Face(Right); }
            if (p.x > maxX) { p.x = maxX; Face(Left); }
            bobPhase += Time.deltaTime * balance.bobFrequency;
            p.y = baseY + Mathf.Sin(bobPhase) * balance.bobAmplitude;
            transform.position = p;
        }
    }
}
