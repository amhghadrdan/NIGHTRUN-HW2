using UnityEngine;
namespace NightRun
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        GameManager game; Camera cam; float shake;
        public Camera Camera => cam;
        public void Initialize(GameManager owner)
        {
            game = owner; gameObject.tag = "MainCamera";
            cam = gameObject.AddComponent<Camera>(); cam.allowHDR = true;
            cam.nearClipPlane = .15f; cam.farClipPlane = 950; cam.fieldOfView = 64;
            gameObject.AddComponent<AudioListener>();
            transform.position = new Vector3(6.2f, 2.4f, -6.6f); transform.LookAt(new Vector3(0, .65f, 0));
        }
        public void Shake() { shake = .4f; }
        void LateUpdate()
        {
            if (game == null || game.Run == null) return;
            bool menu = game.State == GameState.Menu;
            float dt = Time.unscaledDeltaTime;
            Vector3 targetPos, lookAt;
            if (menu)
            {
                float t = Time.unscaledTime * .12f;
                targetPos = new Vector3(4.1f + Mathf.Sin(t) * .25f, 1.65f, -4.6f + Mathf.Cos(t) * .2f);
                // Offset the car to the right, leaving the left column clear for the menu.
                lookAt = new Vector3(-1.8f, .55f, .5f);
            }
            else
            {
                targetPos = new Vector3(game.Car.transform.position.x * .58f, 2.15f, -6.7f);
                lookAt = new Vector3(game.Car.transform.position.x * .72f, 1.9f, 25);
            }
            if (game.State != GameState.Paused && game.State != GameState.GameOver)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, 1 - Mathf.Exp(-dt * 5));
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookAt - transform.position), 1 - Mathf.Exp(-dt * 6));
                if (shake > 0)
                {
                    shake -= dt; transform.position += Random.insideUnitSphere * (shake * .2f);
                }
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, menu ? 48 : (game.Run.Boosting ? 70 : 60), 1 - Mathf.Exp(-dt * 3));
            }
        }
    }
}
