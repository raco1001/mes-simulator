using UnityEngine;

namespace UnityClient.Presentation
{
    /// <summary>
    /// 최소 부트스트랩: Play 모드 진입 시 클라이언트 기동 확인용.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Unity client started.");
        }
    }
}
