﻿//------------------------------------------------------------------------------
// This is auto-generated code.
//------------------------------------------------------------------------------
// This code was generated by Entity Developer tool using EF Core template.
// Code is generated on: 04/03/2025 10:56:51
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace WorkPermitManager.Models
{
    public partial class BusinessType {

        public BusinessType()
        {
            this.Employers = new List<Employer>();
            OnCreated();
        }

        public virtual int BusinesstypeID { get; set; }

        public virtual string BusinesstypeTh { get; set; }

        public virtual string BusinesstypeEng { get; set; }

        public virtual DateTime? CreatedAt { get; set; }

        public virtual DateTime? UpdatedAt { get; set; }

        public virtual bool IsActive { get; set; }

        public virtual int UserManageID { get; set; }

        public virtual IList<Employer> Employers { get; set; }

        public virtual User User { get; set; }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
