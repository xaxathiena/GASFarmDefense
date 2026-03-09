using UnityEngine;

namespace FD.Modules.VFX
{
    public interface IVFXManager
    {
        /// <summary>
        /// Bắn và tự hủy tại một điểm (vd: vụ nổ).
        /// </summary>
        /// <param name="vfxID">ID cấu hình trong VFXConfigSO</param>
        /// <param name="position">Vị trí spawn</param>
        /// <returns>ID nội bộ của hiệu ứng đang chạy (dùng để sửa đổi/xóa sau này)</returns>
        int PlayEffectAt(string vfxID, Vector3 position);

        /// <summary>
        /// Cập nhật vị trí thủ công cho các hiệu ứng cần kéo lê (đạn bay, aura bám theo).
        /// </summary>
        /// <param name="handleID">ID trả về từ hàm PlayEffectAt</param>
        /// <param name="newPosition">Vị trí mới</param>
        void UpdateEffectPosition(int handleID, Vector3 newPosition);

        /// <summary>
        /// Xóa thủ công hoặc ngắt vòng lặp của một hiệu ứng đang chạy.
        /// </summary>
        /// <param name="handleID">ID trả về từ hàm PlayEffectAt</param>
        void StopEffect(int handleID);
    }
}
