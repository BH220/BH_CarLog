namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// POST /api/carlog/consumables/history { car_consumables_num }
    /// 소모품 정보에 변경 이력(history)이 함께 담겨 온다.
    /// </summary>
    public class ResConsumableHistory : ResBase
    {
        public ConsumableInfo? detail { get; set; }
    }
}
