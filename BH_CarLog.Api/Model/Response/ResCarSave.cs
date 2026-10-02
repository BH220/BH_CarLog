namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/car/update (car_num 이 없거나 0 이면 등록)</summary>
    public class ResCarSave : ResBase
    {
        public int car_num { get; set; }

        /// <summary>true 면 신규 등록, false 면 수정</summary>
        public bool created { get; set; }
    }
}
