﻿using Nssol.Platypus.Models;

namespace Nssol.Platypus.ApiModels.RoleApiModels
{
    public class DetailsOutputModel : IndexOutputModel
    {
        public DetailsOutputModel(Role role) : base(role)
        {
            IsNotEditable = role.IsNotEditable;
        }

        /// <summary>
        /// 紐づけられているテナント名
        /// </summary>
        /// <remarks>
        /// <see cref="IndexOutputModel.IsSystemRole"/>がTrueの場合は、必ずNULL
        /// </remarks>
        public string TenantName { get; set; }

        /// <summary>
        /// 編集不可
        /// </summary>
        /// <remarks>
        /// true：編集不可　false：編集可
        /// </remarks>
        public bool IsNotEditable { get; set; }
    }
}
