﻿using OrmTest.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCM.Manager.Models;

namespace OrmTest.BugTest
{
    public class Bug2
    {
        public SqlSugarClient DB
        {
            get
            {
                SqlSugarClient db = new SqlSugarClient(new ConnectionConfig()
                {
                    InitKeyType = InitKeyType.Attribute,
                    ConnectionString = Config.ConnectionString,
                    DbType = DbType.SqlServer,
                    IsAutoCloseConnection = true
                });
                return db;
            }
        }
        public void Init()
        {
            var x2 = DB.Queryable<School>().Where(x => x.Id == SqlFunc.Subqueryable<School>().Where(y => y.Id == SqlFunc.Subqueryable<Student>().Where(yy => y.Id == x.Id).Select(yy => yy.Id)).Select(y => y.Id)).ToSql();
            if (!x2.Key.Contains("STudent"))
            {
                // throw new Exception("bug2 error");
            }



            var UserNameOrName = "111";
            var OrganizationUnitId = 0;
            var RoleId = 0;
            var sql = DB.Queryable<User>().//一对多的子查询
               WhereIF(!string.IsNullOrWhiteSpace(UserNameOrName), t1 => t1.Name.Contains(UserNameOrName)).
               Where(t1 => 
                           SqlFunc.Subqueryable<UserOrganizationUnit>().
                                                        Where(t2 => t2.UserId == t1.Id).
                                                        WhereIF(OrganizationUnitId > 0, t2 => t2.OrganizationUnitId == OrganizationUnitId).Any())
                                                        // Where(t1 => SqlFunc.Subqueryable<UserRole>().
                                                        //Where(t3 => t3.UserId == t1.Id).
                                                        //WhereIF(RoleId > 0, t3 => t3.RoleId == RoleId).Any())
              .Select(t1 => new User { Id = SqlFunc.GetSelfAndAutoFill(t1.Id) }).ToSql();

            var model = DB.Queryable<ClientsModel, VipAccountsModel, AccountsModel, tLogonHistoryModel, VipBenefitsModel, LevelSettingModel, JewelsModel>((a, b, c, d, e, f, g) => new object[]{
                                     JoinType.Left,a.ClientID==b.ClientID,
                                     JoinType.Left,a.ClientID==c.ClientID&&c.TournamentID==0,
                                     JoinType.Left,a.ClientID==d.ClientID,
                                     JoinType.Left,(e.MinVipCredit<=b.VipCredit&&e.MaxVipCredit>=b.VipCredit) && (e.MinConsumeAmount<=b.AccumulatedConsumeAmount&&e.MaxConsumeAmount>=b.AccumulatedConsumeAmount),
                                     JoinType.Left,(c.ExperiencePoints>=f.MinExperiencePoints && c.ExperiencePoints<f.MaxExperiencePoints) || (c.ExperiencePoints > f.MaxExperiencePoints && f.UserLevel== 30),
                                     JoinType.Left,g.ClientID==a.ClientID
                                })
                              .WhereIF(true, (a, b, c, d, e, f, g) => a.ClientID == 1)
                              .WhereIF(!string.IsNullOrEmpty("a"), (a, b, c, d, e, f, g) => a.NickName == "a")
                              .Select((a, b, c, d, e, f, g) => new 
                              {
                                  GoldAmount = SqlFunc.Subqueryable<ExposureModel>().Where(s => s.TournamentID == 0 && s.ClientID == a.ClientID).Sum(s => SqlFunc.IsNull(SqlFunc.AggregateSum(s.Exposure), 0)) ,
                                  ClientID = a.ClientID,
                                  NickName = a.NickName,
                                  UserChannel = a.UserChannel,
                                  CountryCode = d.CountryCode,
                                  Platform = a.Platform,
                                  Email = a.Email,
                                  PhoneNumber = a.PhoneNumber,
                                  RegisteredTime = a.RegisteredTime,
                                  DiamondAmount = SqlFunc.IsNull(g.JewelCount, 0),
                                  AccumulatedRechargeAmount = SqlFunc.IsNull(b.AccumulatedRechargeAmount, 0),
                                  VipLevel = SqlFunc.IsNull(e.VipLevel, 0),
                                  UserLevel = SqlFunc.IsNull(f.UserLevel, 0)
                              })
                              .With(SqlWith.NoLock)
                              .ToSql();

            var _sql = DB.Insertable(new UserInfo
            {
                BrandId = -1,
                UserLevel = 1
            }).IgnoreColumns(m => new { m.BlockingTime, m.CreditUpdatetime }).ToSql();

            var _sql2 = DB.Insertable(new UserInfo
            {
                BrandId = -1,
                UserLevel = 1
            }).IgnoreColumns(m => new { m.UserId }).ToSql();
            var _sql3 = DB.Updateable(new UserInfo
            {
                BrandId = -1,
                UserLevel = 1
            }).IgnoreColumns(m => new { m.CreditUpdatetime,m.UserId }).ToSql();
            DB.CodeFirst.InitTables(typeof(DataTest));
            DB.Insertable(new DataTest()).ExecuteCommand();

            // 初始化实体表
            DB.CodeFirst.SetStringDefaultLength(255).InitTables(typeof(TestA));

            var testa = new TestA();
            testa.Col1 = "2333333";
            testa.Col3 = "444";

            DB.Saveable(testa).ExecuteCommand();
        }
    }

    ///<summary>
    ///用户信息表
    ///</summary>
    public partial class User
    {
        ///<summary>
        /// 描述:主键
        /// 默认值: 
        /// 是否可空: False
        ///</summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        public string Name { get; set; }
    }
    public class Base : ModelContext
    {

        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();


        [SugarColumn(IsNullable = true)]
        public string Col3 { get; set; }
    }

    public class TestA : Base
    {
        public string Col1 { get; set; }

        [SugarColumn(IsIgnore = true)]
        public string Col2 { get; set; }
    }
    public partial class UserOrganizationUnit
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        public long UserId { get; set; }

        public long OrganizationUnitId { get; set; }

    }

    ///<summary>
    ///用户角色关系表
    ///</summary>
    public partial class UserRole
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        public long UserId { get; set; }

        public int RoleId { get; set; }
    }
    /// <summary>
	/// VmallUser 实体
	/// </summary>
	[SugarTable("vmall_user")]
    public class UserInfo
    {
        #region 属性
    
        public int UserId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [SugarColumn(ColumnName = "brand_id")]
        public int BrandId { get; set; }
        /// <summary>
        /// 用户等级1普通 2高级 0黑名单
        /// </summary>
        [SugarColumn(ColumnName = "user_level")]
        public byte UserLevel { get; set; }
        /// <summary>
        /// 拉黑时间
        /// </summary>
        [SugarColumn(ColumnName = "blocking_time")]
        public DateTime BlockingTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [SugarColumn(ColumnName = "credit_updatetime")]
        public DateTime CreditUpdatetime { get; set; }
        #endregion
    }

    public class DataTest {
        [SugarColumn( ColumnDataType = "time",IsNullable =true)]
         public TimeSpan? dateTime { get; set; }
    }
}
 
