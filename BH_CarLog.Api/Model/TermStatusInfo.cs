using BH_CarLog.Api.Model.Response;

namespace BH_CarLog.Api.Model
{
    /// <summary>교환 필요 여부 (기존 frmTermList 의 IS_REQUIRED_CHANGE)</summary>
    public enum TermStatus
    {
        /// <summary>관리 중인 주기·거리가 아직 남아 있다</summary>
        정상,

        /// <summary>주기·거리 중 일부만 지났다</summary>
        확인필요,

        /// <summary>관리 중인 주기·거리가 모두 지났다</summary>
        교환필요,

        /// <summary>교체이력이 없어 계산할 수 없다</summary>
        확인불가,
    }

    /// <summary>
    /// 교환주기 화면 한 행. 소모품 항목 + 교체이력 + 차량의 최근 주행거리를 합쳐 클라이언트에서 계산한다.
    /// (기존 SqlManager.SELECT_CHANGE_PERIOD_DETAILS 가 SQL 로 하던 계산)
    /// </summary>
    public class TermStatusInfo
    {
        public int car_consumables_num { get; init; }
        public int car_num { get; init; }
        public string name { get; init; } = "";

        /// <summary>관리 주기(일). null 이면 주기로는 관리하지 않는다</summary>
        public int? term { get; init; }

        /// <summary>관리 주기(거리 km). null 이면 거리로는 관리하지 않는다</summary>
        public int? distance { get; init; }

        /// <summary>교체이력 건수 (교환횟수)</summary>
        public int change_count { get; init; }

        /// <summary>최근 교환일 (연결된 유지보수 기록 중 가장 늦은 유지보수일 mataintenance_at)</summary>
        public DateTime? last_changed_at { get; init; }

        /// <summary>최근 교환 시 주행거리 (연결된 유지보수 기록 중 최대 주행거리)</summary>
        public int? last_changed_mileage { get; init; }

        /// <summary>차량의 최근 주행거리 (주유·유지보수 기록 중 최대값)</summary>
        public int current_mileage { get; init; }

        /// <summary>남은 주기(일). 음수면 지났다. 주기를 관리하지 않거나 이력이 없으면 null</summary>
        public int? days_left { get; init; }

        /// <summary>남은 거리(km). 음수면 지났다. 거리를 관리하지 않거나 이력이 없으면 null</summary>
        public int? distance_left { get; init; }

        public TermStatus Status { get; init; }

        #region 화면 표시용
        public string TermText => term.HasValue ? $"{term.Value:#,0}일" : "-";
        public string DistanceText => distance.HasValue ? $"{distance.Value:#,0}㎞" : "-";
        public string CountText => $"{change_count}회";
        public string LastMileageText => last_changed_mileage.HasValue ? $"{last_changed_mileage.Value:#,0}㎞" : "-";
        public string DaysLeftText => days_left.HasValue ? $"{days_left.Value:#,0}일" : "-";
        public string DistanceLeftText => distance_left.HasValue ? $"{distance_left.Value:#,0}㎞" : "-";
        #endregion

        /// <summary>
        /// 소모품 한 건의 상태를 계산한다.
        /// 남은 주기 = 주기 - (최근 교환일부터 오늘까지 일수 + 1), 남은 거리 = 거리 - (현재 주행거리 - 최근 교환 주행거리).
        /// 관리하는 항목(주기/거리)이 모두 지났으면 교환필요, 일부만 지났으면 확인필요, 이력이 없으면 확인불가.
        /// </summary>
        public static TermStatusInfo Create(ConsumableInfo consumable, IReadOnlyCollection<ConsumableHistoryInfo> history, int currentMileage, DateTime today)
        {
            DateTime? lastAt = null;
            int? lastMileage = null;
            if (history.Count > 0)
            {
                // 유지보수일 기준. (TermManager 가 history 응답에 없는 유지보수일을 유지보수 목록에서 채워 둔다)
                lastAt = history.Max(x => x.MaintenanceAt);
                lastMileage = history.Max(x => x.mileage);
            }

            int? daysLeft = null;
            if (consumable.term is > 0 && lastAt.HasValue)
                daysLeft = consumable.term.Value - ((today.Date - lastAt.Value.Date).Days + 1);

            int? distanceLeft = null;
            if (consumable.distance is > 0 && lastMileage.HasValue)
            {
                int driven = Math.Max(0, currentMileage - lastMileage.Value);
                distanceLeft = consumable.distance.Value - driven;
            }

            TermStatus status;
            if (history.Count == 0 || (lastAt == null && lastMileage == null))
            {
                status = TermStatus.확인불가;
            }
            else
            {
                int tracked = (daysLeft.HasValue ? 1 : 0) + (distanceLeft.HasValue ? 1 : 0);
                int over = (daysLeft < 0 ? 1 : 0) + (distanceLeft < 0 ? 1 : 0);
                status = over == 0 ? TermStatus.정상
                       : over == tracked ? TermStatus.교환필요
                       : TermStatus.확인필요;
            }

            return new TermStatusInfo
            {
                car_consumables_num = consumable.car_consumables_num,
                car_num = consumable.car_num,
                name = consumable.name,
                term = consumable.term,
                distance = consumable.distance,
                change_count = history.Count,
                last_changed_at = lastAt,
                last_changed_mileage = lastMileage,
                current_mileage = currentMileage,
                days_left = daysLeft,
                distance_left = distanceLeft,
                Status = status,
            };
        }
    }
}
