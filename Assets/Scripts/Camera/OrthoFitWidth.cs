using Unity.VisualScripting;
using UnityEngine;
// 横幅基準で合わせる例（2D/Orthographic）
public class OrthoFitWidth : MonoBehaviour {
    [SerializeField] private Camera _cam;
    [SerializeField] private float targetHalfWidth = 5f; // ワールド単位で「画面の半分の横幅」を固定したい値

    void LateUpdate() {
        _cam.orthographicSize = targetHalfWidth / _cam.aspect; // aspectで横幅が決まる [web:55]
    }
}
