using UnityEngine;

namespace SceneTokenAudio.Research
{
    public sealed class AssemblyCaptureDebugUI : MonoBehaviour
    {
        public AssemblyCaptureRecorder recorder;
        public bool show = true;
        private void OnGUI()
        {
            if (!show || !recorder) return;
            GUILayout.BeginArea(new Rect(12, 12, 650, 240), GUI.skin.box);
            GUILayout.Label("ドローン組立：操作ログの記録状況");
            GUILayout.Label("記録したイベント数：" + recorder.RecordsWritten + " 件");
            var binder = GetComponent<DroneCaptureRuntimeBinder>();
            if (binder) GUILayout.Label("接続した記録対象：部品 " + binder.BoundParts + " 個 ／ 作業工程 " + binder.BoundActivities + " 件");
            GUILayout.Label("聞き手の位置参照：" + (recorder.audioListener ? recorder.audioListener.name : "未取得"));
            GUILayout.Label("ログ保存先：" + (recorder.DirectoryPath ?? "最初のイベントを待っています"));
            GUILayout.Label("CSVファイル名：events-audio.csv");
            GUILayout.Label("部品をつかむ・離すなどの操作をすると、イベントを記録します。");
            if (!string.IsNullOrEmpty(recorder.LastError)) GUILayout.Label("記録エラー：" + recorder.LastError);
            GUILayout.EndArea();
        }
    }
}
