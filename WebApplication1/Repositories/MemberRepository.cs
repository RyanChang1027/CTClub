using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using CrownTowersClub.Domain;
using CrownTowersClub.Infrastructure.Data;
using CrownTowersClub.Models;
using CrownTowersClub.ViewModels;

namespace CrownTowersClub.Repositories
{
    public sealed class MemberRepository : IMemberRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        // The default annual fee. In the future, this should be pulled from a Settings table.
        private const decimal DefaultAnnualFee = 24.00m;

        public MemberRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        // Shared SELECT used to join Members with their Membership and current Term.
        private const string BaseSelectSql = @"
            SELECT 
                m.*,
                ms.MembershipId,
                ms.MembershipNumber,
                ms.MembershipStatus,
                ms.CancellationReason,
                ms.AdmissionDate,
                mt.TermStatus AS CurrentTermStatus,
                mt.MembershipYear
            FROM dbo.Members m
            LEFT JOIN dbo.Memberships ms ON m.MemberId = ms.MemberId
            LEFT JOIN dbo.MembershipTerms mt ON ms.MembershipId = mt.MembershipId 
                AND mt.MembershipYear = YEAR(GETDATE())";

        #region 1. Read Actions

        public Member GetById(int id)
        {
            string sql = BaseSelectSql + " WHERE m.MemberId = @id;";
            using (IDbConnection db = connectionFactory.Create())
                return db.QuerySingleOrDefault<Member>(sql, new { id });
        }

        public Member GetByMembershipNumber(string membershipNumber)
        {
            string sql = BaseSelectSql +
                " WHERE ms.MembershipNumber = @membershipNumber;";

            using (IDbConnection db = connectionFactory.Create())
                return db.QuerySingleOrDefault<Member>(
                    sql,
                    new { membershipNumber });
        }

        public IReadOnlyList<Member> GetActiveMembersForSeatAssignment()
        {
            string sql = BaseSelectSql + @"
                WHERE ms.MembershipStatus = 'Active'
                  AND mt.TermStatus = 'Active'
                  AND CAST(GETDATE() AS DATE)
                      BETWEEN mt.StartDate AND mt.EndDate
                ORDER BY m.Name ASC;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Query<Member>(sql).ToList();
        }

        public IReadOnlyList<Member> GetByNric(string nric)
        {
            string sql = BaseSelectSql + @"
                WHERE m.NRIC = @nric
                ORDER BY m.ApplicationDate DESC, m.MemberId DESC;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Query<Member>(sql, new { nric }).ToList();
        }

        public Member GetDraftByNric(string nric)
        {
            const string sql = @"
                SELECT TOP 1 *
                FROM dbo.Members
                WHERE NRIC = @nric
                  AND ApplicationStatus IN ('Draft', 'Returned for Correction')
                ORDER BY UpdatedAt DESC, MemberId DESC;";

            using (IDbConnection db = connectionFactory.Create())
                return db.QuerySingleOrDefault<Member>(sql, new { nric });
        }

        public bool ExistsByNric(string nric, int? excludingId = null)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Members
                WHERE NRIC = @nric
                  AND ApplicationStatus NOT IN ('Draft', 'Returned for Correction')
                  AND (@excludingId IS NULL OR MemberId <> @excludingId);";

            using (IDbConnection db = connectionFactory.Create())
                return db.ExecuteScalar<int>(sql, new { nric, excludingId }) > 0;
        }

        public IReadOnlyList<MembershipTerm> GetTermsByMembershipId(int membershipId)
        {
            const string sql = @"
                SELECT *
                FROM dbo.MembershipTerms
                WHERE MembershipId = @membershipId
                ORDER BY MembershipYear DESC;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Query<MembershipTerm>(sql, new { membershipId }).ToList();
        }

        public IReadOnlyList<Payment> GetPaymentsByMemberId(int memberId)
        {
            const string sql = @"
                SELECT 
                    p.*,
                    mt.MembershipYear,
                    ms.MemberId AS MemberId
                FROM dbo.Payments p
                INNER JOIN dbo.MembershipTerms mt ON p.MembershipTermId = mt.MembershipTermId
                INNER JOIN dbo.Memberships ms ON mt.MembershipId = ms.MembershipId
                WHERE ms.MemberId = @memberId
                ORDER BY p.PaymentDate DESC;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Query<Payment>(sql, new { memberId }).ToList();
        }

        #endregion


        #region 2. Write Actions

        public int Insert(Member member)
        {
            const string sql = @"
                INSERT INTO dbo.Members 
                    (Name, NRIC, Gender, Race, MaritalStatus, Nationality, DateOfBirth, PlaceOfBirth, 
                     CompanyName, CompanyAddress, Department, Occupation, HeadOfDepartment, Floor, 
                     Tel, Email, HomeAddress, Hobbies, Declaration1, Declaration2, 
                     ApplicationDate, ApplicationStatus, CreatedAt)
                VALUES 
                    (@Name, @NRIC, @Gender, @Race, @MaritalStatus, @Nationality, @DateOfBirth, @PlaceOfBirth, 
                     @CompanyName, @CompanyAddress, @Department, @Occupation, @HeadOfDepartment, @Floor, 
                     @Tel, @Email, @HomeAddress, @Hobbies, @Declaration1, @Declaration2, 
                     GETDATE(), @ApplicationStatus, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            using (IDbConnection db = connectionFactory.Create())
                return db.ExecuteScalar<int>(sql, member);
        }

        public int InsertDraft(Member member)
        {
            const string sql = @"
                INSERT INTO dbo.Members 
                    (Name, NRIC, Gender, Race, MaritalStatus, Nationality, DateOfBirth, PlaceOfBirth, 
                     CompanyName, CompanyAddress, Department, Occupation, HeadOfDepartment, Floor, 
                     Tel, Email, HomeAddress, Hobbies, Declaration1, Declaration2, 
                     ApplicationDate, ApplicationStatus, CreatedAt)
                VALUES 
                    (@Name, @NRIC, @Gender, @Race, @MaritalStatus, @Nationality, @DateOfBirth, @PlaceOfBirth, 
                     @CompanyName, @CompanyAddress, @Department, @Occupation, @HeadOfDepartment, @Floor, 
                     @Tel, @Email, @HomeAddress, @Hobbies, @Declaration1, @Declaration2, 
                     GETDATE(), 'Draft', GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            using (IDbConnection db = connectionFactory.Create())
                return db.ExecuteScalar<int>(sql, member);
        }

        public bool Update(Member member)
        {
            const string sql = @"
                UPDATE dbo.Members
                SET
                    Name = @Name, NRIC = @NRIC, Gender = @Gender, Race = @Race,
                    MaritalStatus = @MaritalStatus, Nationality = @Nationality,
                    DateOfBirth = @DateOfBirth, PlaceOfBirth = @PlaceOfBirth,
                    CompanyName = @CompanyName, CompanyAddress = @CompanyAddress,
                    Department = @Department, Occupation = @Occupation,
                    HeadOfDepartment = @HeadOfDepartment, Floor = @Floor,
                    Tel = @Tel, Email = @Email, HomeAddress = @HomeAddress, Hobbies = @Hobbies,
                    Declaration1 = @Declaration1, Declaration2 = @Declaration2,
                    ApplicationStatus = @ApplicationStatus,
                    UpdatedAt = GETDATE()
                WHERE MemberId = @MemberId;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Execute(sql, member) == 1;
        }

        public bool UpdateDraft(Member member)
        {
            const string sql = @"
                UPDATE dbo.Members
                SET
                    Name = @Name, NRIC = @NRIC, Gender = @Gender, Race = @Race,
                    MaritalStatus = @MaritalStatus, Nationality = @Nationality,
                    DateOfBirth = @DateOfBirth, PlaceOfBirth = @PlaceOfBirth,
                    CompanyName = @CompanyName, CompanyAddress = @CompanyAddress,
                    Department = @Department, Occupation = @Occupation,
                    HeadOfDepartment = @HeadOfDepartment, Floor = @Floor,
                    Tel = @Tel, Email = @Email, HomeAddress = @HomeAddress, Hobbies = @Hobbies,
                    Declaration1 = @Declaration1, Declaration2 = @Declaration2,
                    ApplicationStatus = CASE 
                        WHEN ApplicationStatus = 'Returned for Correction' 
                            THEN 'Returned for Correction' 
                        ELSE 'Draft' 
                    END,
                    UpdatedAt = GETDATE()
                WHERE MemberId = @MemberId
                  AND ApplicationStatus IN ('Draft', 'Returned for Correction');";

            using (IDbConnection db = connectionFactory.Create())
                return db.Execute(sql, member) == 1;
        }

        public bool SubmitDraftWithStatus(Member member, string targetStatus)
        {
            const string sql = @"
                UPDATE dbo.Members
                SET
                    Name = @Name, NRIC = @NRIC, Gender = @Gender, Race = @Race,
                    MaritalStatus = @MaritalStatus, Nationality = @Nationality,
                    DateOfBirth = @DateOfBirth, PlaceOfBirth = @PlaceOfBirth,
                    CompanyName = @CompanyName, CompanyAddress = @CompanyAddress,
                    Department = @Department, Occupation = @Occupation,
                    HeadOfDepartment = @HeadOfDepartment, Floor = @Floor,
                    Tel = @Tel, Email = @Email, HomeAddress = @HomeAddress, Hobbies = @Hobbies,
                    Declaration1 = @Declaration1, Declaration2 = @Declaration2,
                    ApplicationStatus = @targetStatus,
                    ApplicationDate = GETDATE(),
                    CorrectionReason = NULL,
                    UpdatedAt = GETDATE()
                WHERE MemberId = @MemberId
                  AND ApplicationStatus IN ('Draft', 'Returned for Correction');";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.Execute(sql, new
                {
                    member.MemberId,
                    member.Name,
                    member.NRIC,
                    member.Gender,
                    member.Race,
                    member.MaritalStatus,
                    member.Nationality,
                    member.DateOfBirth,
                    member.PlaceOfBirth,
                    member.CompanyName,
                    member.CompanyAddress,
                    member.Department,
                    member.Occupation,
                    member.HeadOfDepartment,
                    member.Floor,
                    member.Tel,
                    member.Email,
                    member.HomeAddress,
                    member.Hobbies,
                    member.Declaration1,
                    member.Declaration2,
                    targetStatus
                }) == 1;
            }
        }

        public bool Delete(int id)
        {
            using (IDbConnection db = connectionFactory.Create())
                return db.Execute("DELETE FROM dbo.Members WHERE MemberId = @id;", new { id }) == 1;
        }

        #endregion


        #region 3. Department Head Actions

        public int GetPendingByHodCount(string hodNric)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Members m
                INNER JOIN [dbo].[fnHOD_Staff_Get](null) h
                    ON m.NRIC COLLATE Latin1_General_CI_AI = h.[staff ic]
                WHERE m.ApplicationStatus = 'Pending for Verification'
                  AND h.[hod ic] = @HodNric;";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.ExecuteScalar<int>(
                    sql,
                    new { HodNric = hodNric });
            }
        }

        public ApplicationListViewModel GetPendingByHodPaged(
            string hodNric,
            int page,
            int pageSize)
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                const string countSql = @"
                    SELECT COUNT(1)
                    FROM dbo.Members m
                    INNER JOIN [dbo].[fnHOD_Staff_Get](null) h
                        ON m.NRIC COLLATE Latin1_General_CI_AI = h.[staff ic]
                    WHERE m.ApplicationStatus = 'Pending for Verification'
                      AND h.[hod ic] = @HodNric;";

                const string dataSql = @"
                    SELECT
                        m.*,
                        ms.MembershipId,
                        ms.MembershipNumber,
                        ms.MembershipStatus,
                        mt.TermStatus AS CurrentTermStatus
                    FROM dbo.Members m
                    INNER JOIN [dbo].[fnHOD_Staff_Get](null) h
                        ON m.NRIC COLLATE Latin1_General_CI_AI = h.[staff ic]
                    LEFT JOIN dbo.Memberships ms
                        ON m.MemberId = ms.MemberId
                    LEFT JOIN dbo.MembershipTerms mt
                        ON ms.MembershipId = mt.MembershipId
                        AND mt.MembershipYear = YEAR(GETDATE())
                    WHERE m.ApplicationStatus = 'Pending for Verification'
                      AND h.[hod ic] = @HodNric
                    ORDER BY m.ApplicationDate ASC, m.MemberId ASC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                return GetPagedApplications(
                    db,
                    countSql,
                    dataSql,
                    new { HodNric = hodNric },
                    page,
                    pageSize);
            }
        }

        public bool VerifyByDeptHead(int memberId, string expectedDepartment, string changedBy)
        {
            const string sql = @"
                UPDATE dbo.Members
                SET ApplicationStatus = 'Verified', UpdatedAt = GETDATE()
                WHERE MemberId = @memberId
                  AND ApplicationStatus = 'Pending for Verification'
                  AND Department = @expectedDepartment;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Execute(sql, new { memberId, expectedDepartment }) == 1;
        }

        public bool RejectByDeptHead(int memberId, string expectedDepartment, string reason, string changedBy)
        {
            const string sql = @"
                UPDATE dbo.Members
                SET ApplicationStatus = 'Rejected by Dept Head',
                    RejectionReason = @reason, RejectedBy = @changedBy, RejectedAt = GETDATE(),
                    UpdatedAt = GETDATE()
                WHERE MemberId = @memberId
                  AND ApplicationStatus = 'Pending for Verification'
                  AND Department = @expectedDepartment;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Execute(sql, new { memberId, expectedDepartment, reason, changedBy }) == 1;
        }

        public bool ReturnByDeptHead(int memberId, string expectedDepartment, string reason, string changedBy)
        {
            const string sql = @"
                UPDATE dbo.Members
                SET ApplicationStatus = 'Returned for Correction',
                    CorrectionReason = @reason,
                    UpdatedAt = GETDATE()
                WHERE MemberId = @memberId
                  AND ApplicationStatus = 'Pending for Verification'
                  AND Department = @expectedDepartment;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Execute(sql, new { memberId, expectedDepartment, reason }) == 1;
        }

        #endregion


        #region 4. Committee Actions

        public int GetVerifiedCount()
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Members
                WHERE ApplicationStatus = 'Verified';";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.ExecuteScalar<int>(sql);
            }
        }

        public ApplicationListViewModel GetVerifiedPaged(
    int page,
    int pageSize)
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                const string countSql = @"
                    SELECT COUNT(1)
                    FROM dbo.Members
                    WHERE ApplicationStatus = 'Verified';";

                const string dataSql = @"
                    SELECT *
                    FROM dbo.Members
                    WHERE ApplicationStatus = 'Verified'
                    ORDER BY ApplicationDate ASC, MemberId ASC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                return GetPagedApplications(
                    db,
                    countSql,
                    dataSql,
                    null,
                    page,
                    pageSize);
            }
        }

        public string ApproveByCommittee(int memberId, string changedBy)
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                db.Open();
                using (IDbTransaction transaction = db.BeginTransaction())
                {
                    try
                    {
                        // 1. Verify status
                        const string checkSql = "SELECT ApplicationStatus FROM dbo.Members WHERE MemberId = @memberId;";
                        string currentStatus = db.QuerySingleOrDefault<string>(checkSql, new { memberId }, transaction);
                        if (currentStatus != "Verified") return null;

                        // 2. Update Application Status
                        const string updateAppSql = @"
                            UPDATE dbo.Members SET ApplicationStatus = 'Approved', UpdatedAt = GETDATE() 
                            WHERE MemberId = @memberId;";
                        db.Execute(updateAppSql, new { memberId }, transaction);

                        // 3. Generate Membership Number
                        int currentYear = DateTime.Now.Year;
                        string yearPrefix = currentYear.ToString() + "/";
                        const string getLastNumSql = @"
                            SELECT TOP 1 MembershipNumber FROM dbo.Memberships 
                            WHERE MembershipNumber LIKE @yearPrefix + '%' ORDER BY MembershipNumber DESC;";
                        string lastNumber = db.QuerySingleOrDefault<string>(getLastNumSql, new { yearPrefix }, transaction);

                        int nextRunningNumber = 1;
                        if (!string.IsNullOrEmpty(lastNumber))
                        {
                            string[] parts = lastNumber.Split('/');
                            int parsedNum;
                            if (parts.Length == 2 && int.TryParse(parts[1], out parsedNum))
                                nextRunningNumber = parsedNum + 1;
                        }

                        // Format as 2026/001
                        string newMembershipNumber = $"{currentYear}/{nextRunningNumber:D3}";

                        // 4. Insert Membership
                        const string insertMembershipSql = @"
                            INSERT INTO dbo.Memberships (MemberId, MembershipNumber, MembershipStatus, AdmissionDate)
                            VALUES (@memberId, @newMembershipNumber, 'Pending Activation', CAST(GETDATE() AS DATE));
                            SELECT CAST(SCOPE_IDENTITY() AS int);";
                        int newMembershipId = db.ExecuteScalar<int>(insertMembershipSql, new { memberId, newMembershipNumber }, transaction);

                        // 5. Insert First Term
                        DateTime startDate = new DateTime(currentYear, 1, 1);
                        DateTime endDate = new DateTime(currentYear, 12, 31);
                        DateTime dueDate = new DateTime(currentYear, 3, 31);

                        const string insertTermSql = @"
                            INSERT INTO dbo.MembershipTerms 
                                (MembershipId, MembershipYear, StartDate, EndDate, FeeAmount, DueDate, TermStatus)
                            VALUES 
                                (@newMembershipId, @currentYear, @startDate, @endDate, @DefaultAnnualFee, @dueDate, 'Pending Payment');";
                        db.Execute(insertTermSql, new { newMembershipId, currentYear, startDate, endDate, DefaultAnnualFee, dueDate }, transaction);

                        transaction.Commit();
                        return newMembershipNumber;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool RejectByCommittee(int memberId, string reason, string changedBy)
        {
            const string sql = @"
                UPDATE dbo.Members
                SET ApplicationStatus = 'Rejected by Committee',
                    RejectionReason = @reason, RejectedBy = @changedBy, RejectedAt = GETDATE(),
                    UpdatedAt = GETDATE()
                WHERE MemberId = @memberId AND ApplicationStatus = 'Verified';";

            using (IDbConnection db = connectionFactory.Create())
                return db.Execute(sql, new { memberId, reason, changedBy }) == 1;
        }

        public bool CancelMembership(int memberId, string reason, string changedBy)
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                db.Open();
                using (IDbTransaction transaction = db.BeginTransaction())
                {
                    try
                    {
                        // 1. Update the Membership to Cancelled
                        const string updateMembershipSql = @"
                        UPDATE dbo.Memberships
                        SET MembershipStatus = 'Cancelled',
                            CancellationReason = @reason,
                            UpdatedAt = GETDATE()
                        WHERE MemberId = @memberId
                          AND MembershipStatus IN ('Active', 'Suspended', 'Pending Activation');";

                        int rowsAffected = db.Execute(updateMembershipSql, new { memberId, reason }, transaction);

                        if (rowsAffected == 0)
                        {
                            return false; // No active membership found for this member
                        }

                        // 2. Expire any current 'Active' or 'Pending Payment' terms
                        const string updateTermsSql = @"
                        UPDATE mt
                        SET TermStatus = 'Expired', 
                            UpdatedAt = GETDATE()
                        FROM dbo.MembershipTerms mt
                        INNER JOIN dbo.Memberships ms ON mt.MembershipId = ms.MembershipId
                        WHERE ms.MemberId = @memberId
                          AND mt.TermStatus IN ('Active', 'Pending Payment');";

                        db.Execute(updateTermsSql, new { memberId }, transaction);

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion


        #region 5. Payments & Renewals

        public PaymentDashboardViewModel GetPaymentDashboard(string tab, int year)
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                var viewModel = new PaymentDashboardViewModel
                {
                    CurrentTab = tab,
                    SelectedYear = year
                };

                // 1. Dashboard Statistics
                const string statsSql = @"
                    SELECT
                        ISNULL(SUM(CASE
                            WHEN mt.TermStatus = 'Active' THEN p.Amount
                            ELSE 0
                        END), 0) AS TotalCollected,

                        ISNULL(SUM(CASE
                            WHEN mt.TermStatus = 'Pending Payment' THEN mt.FeeAmount
                            ELSE 0
                        END), 0) AS TotalOutstanding,

                        COUNT(CASE
                            WHEN mt.TermStatus = 'Pending Payment' THEN 1
                        END) AS PendingCount

                    FROM dbo.MembershipTerms mt
                    LEFT JOIN dbo.Payments p
                        ON mt.MembershipTermId = p.MembershipTermId
                    WHERE mt.MembershipYear = @Year;";

                var stats = db.QuerySingle<dynamic>(
                    statsSql,
                    new { Year = year }
                );

                viewModel.TotalCollected = (decimal)stats.TotalCollected;
                viewModel.TotalOutstanding = (decimal)stats.TotalOutstanding;
                viewModel.PendingCount = (int)stats.PendingCount;

                // 2. Load both payment lists.
                const string historySql = @"
                    SELECT
                        p.*,
                        mt.MembershipYear,
                        m.Name,
                        ms.MembershipNumber,
                        ms.MemberId
                    FROM dbo.Payments p
                    INNER JOIN dbo.MembershipTerms mt
                        ON p.MembershipTermId = mt.MembershipTermId
                    INNER JOIN dbo.Memberships ms
                        ON mt.MembershipId = ms.MembershipId
                    INNER JOIN dbo.Members m
                        ON ms.MemberId = m.MemberId
                    WHERE mt.MembershipYear = @Year
                    ORDER BY p.PaymentDate DESC;";

                viewModel.PaymentHistory = db.Query<Payment>(
                    historySql,
                    new { Year = year }
                ).ToList();

                viewModel.PendingPayments = GetPendingPayments(year);

                return viewModel;
            }
        }

        public IReadOnlyList<PendingPaymentViewModel> GetPendingPayments(int year)
        {
            const string sql = @"
                SELECT 
                    m.MemberId, mt.MembershipTermId, ms.MembershipNumber, m.Name, m.NRIC, m.Department,
                    mt.MembershipYear, mt.FeeAmount, mt.TermStatus, ms.MembershipStatus
                FROM dbo.MembershipTerms mt
                INNER JOIN dbo.Memberships ms ON mt.MembershipId = ms.MembershipId
                INNER JOIN dbo.Members m ON ms.MemberId = m.MemberId
                WHERE mt.TermStatus = 'Pending Payment' AND mt.MembershipYear = @Year
                ORDER BY ms.MembershipNumber ASC;";

            using (IDbConnection db = connectionFactory.Create())
                return db.Query<PendingPaymentViewModel>(sql, new { Year = year }).ToList();
        }

        public string RecordPayment(int membershipTermId, string paymentMethod, string remarks, decimal amount)
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                db.Open();
                using (IDbTransaction transaction = db.BeginTransaction())
                {
                    try
                    {
                        // Verify still pending
                        const string checkSql = "SELECT TermStatus FROM dbo.MembershipTerms WHERE MembershipTermId = @membershipTermId;";
                        string currentStatus = db.QuerySingleOrDefault<string>(checkSql, new { membershipTermId }, transaction);
                        if (currentStatus != "Pending Payment") return null;

                        // Generate Receipt Number
                        int currentYear = DateTime.Now.Year;
                        string yearPrefix = $"RT-{currentYear}/";
                        const string getLastNumSql = @"
                            SELECT TOP 1 ReceiptNumber FROM dbo.Payments 
                            WHERE ReceiptNumber LIKE @yearPrefix + '%' ORDER BY PaymentId DESC;";
                        string lastReceipt = db.QuerySingleOrDefault<string>(getLastNumSql, new { yearPrefix }, transaction);

                        int nextRunningNumber = 1;
                        if (!string.IsNullOrEmpty(lastReceipt))
                        {
                            // Extract the running number from 'RT-2026/001'
                            string[] parts = lastReceipt.Split('/');
                            int parsedNum;
                            if (parts.Length == 2 && int.TryParse(parts[1], out parsedNum))
                                nextRunningNumber = parsedNum + 1;
                        }

                        // Format as RT-2026/001
                        string newReceiptNumber = $"{yearPrefix}{nextRunningNumber:D3}";

                        // Insert Payment
                        const string insertPaymentSql = @"
                            INSERT INTO dbo.Payments (MembershipTermId, Amount, PaymentMethod, PaymentDate, ReceiptNumber, Remarks)
                            VALUES (@membershipTermId, @amount, @paymentMethod, GETDATE(), @newReceiptNumber, @remarks);";
                        db.Execute(insertPaymentSql, new { membershipTermId, amount, paymentMethod, newReceiptNumber, remarks }, transaction);

                        // Update Term
                        const string updateTermSql = @"
                            UPDATE dbo.MembershipTerms SET TermStatus = 'Active', UpdatedAt = GETDATE() 
                            WHERE MembershipTermId = @membershipTermId;";
                        db.Execute(updateTermSql, new { membershipTermId }, transaction);

                        // Update Membership
                        const string updateMembershipSql = @"
                            UPDATE ms SET ms.MembershipStatus = 'Active', ms.UpdatedAt = GETDATE()
                            FROM dbo.Memberships ms
                            INNER JOIN dbo.MembershipTerms mt ON ms.MembershipId = mt.MembershipId
                            WHERE mt.MembershipTermId = @membershipTermId;";
                        db.Execute(updateMembershipSql, new { membershipTermId }, transaction);

                        transaction.Commit();
                        return newReceiptNumber;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public int GenerateRenewals(int year, decimal feeAmount)
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                db.Open();
                using (IDbTransaction transaction = db.BeginTransaction())
                {
                    try
                    {
                        // Get Active / Suspended Memberships
                        const string getMembershipsSql = @"
                            SELECT MembershipId FROM dbo.Memberships 
                            WHERE MembershipStatus IN ('Active', 'Suspended');";
                        var activeMemberships = db.Query<int>(getMembershipsSql, transaction: transaction).ToList();

                        int generatedCount = 0;

                        // Insert a Pending Payment term for each if it doesn't already exist
                        const string insertTermSql = @"
                            IF NOT EXISTS (
                                SELECT 1 FROM dbo.MembershipTerms 
                                WHERE MembershipId = @MembershipId AND MembershipYear = @Year
                            )
                            BEGIN
                                INSERT INTO dbo.MembershipTerms 
                                    (MembershipId, MembershipYear, StartDate, EndDate, FeeAmount, DueDate, TermStatus)
                                VALUES 
                                    (@MembershipId, @Year, @StartDate, @EndDate, @FeeAmount, @DueDate, 'Pending Payment');
                                SELECT 1;
                            END
                            ELSE
                            BEGIN
                                SELECT 0;
                            END";

                        var startDate = new DateTime(year, 1, 1);
                        var endDate = new DateTime(year, 12, 31);
                        var dueDate = new DateTime(year, 3, 31);

                        foreach (var membershipId in activeMemberships)
                        {
                            int inserted = db.ExecuteScalar<int>(insertTermSql, new
                            {
                                MembershipId = membershipId,
                                Year = year,
                                StartDate = startDate,
                                EndDate = endDate,
                                FeeAmount = feeAmount,
                                DueDate = dueDate
                            }, transaction);

                            generatedCount += inserted;
                        }

                        transaction.Commit();
                        return generatedCount;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public int ProcessOverdueTerms()
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                db.Open();
                using (IDbTransaction transaction = db.BeginTransaction())
                {
                    try
                    {
                        // Expire pending terms past their due date
                        const string expireTermsSql = @"
                            UPDATE dbo.MembershipTerms
                            SET TermStatus = 'Expired', UpdatedAt = GETDATE()
                            WHERE TermStatus = 'Pending Payment' 
                              AND DueDate < CAST(GETDATE() AS DATE);";
                        int termsUpdated = db.Execute(expireTermsSql, transaction: transaction);

                        // Suspend memberships with expired terms and no active terms
                        const string suspendMembershipsSql = @"
                            UPDATE ms
                            SET ms.MembershipStatus = 'Suspended', ms.UpdatedAt = GETDATE()
                            FROM dbo.Memberships ms
                            WHERE ms.MembershipStatus = 'Active'
                              AND EXISTS (
                                  SELECT 1 FROM dbo.MembershipTerms mt 
                                  WHERE mt.MembershipId = ms.MembershipId AND mt.TermStatus = 'Expired'
                              )
                              AND NOT EXISTS (
                                  SELECT 1 FROM dbo.MembershipTerms mt 
                                  WHERE mt.MembershipId = ms.MembershipId 
                                    AND mt.TermStatus = 'Active'
                                    AND GETDATE() BETWEEN mt.StartDate AND mt.EndDate
                              );";
                        db.Execute(suspendMembershipsSql, transaction: transaction);

                        transaction.Commit();
                        return termsUpdated;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion


        #region 6. Directory (Search & Pagination)

        public DirectoryViewModel GetDirectory(string searchTerm, string tabFilter, int page, int pageSize, int year)
        {
            using (IDbConnection db = connectionFactory.Create())
            {
                page = Math.Max(1, page);
                pageSize = Math.Max(1, pageSize);

                var searchParam = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
                var tabParam = tabFilter ?? "All";

                // Shared directory filters used by all three queries.
                string directoryFilterSql = @"
                    WHERE (@SearchTerm IS NULL
                        OR m.Name LIKE '%' + @SearchTerm + '%'
                        OR m.NRIC LIKE '%' + @SearchTerm + '%')
                      AND (
                          m.ApplicationStatus NOT IN ('Approved')
                          OR (
                              m.ApplicationStatus = 'Approved'
                              AND mt.MembershipTermId IS NOT NULL
                          )
                      )";

                // 1. Tab Counts (Year-Aware)
                string countSql = $@"
                    SELECT 
                        COUNT(DISTINCT m.MemberId) AS CountAll,
                        SUM(CASE WHEN m.ApplicationStatus IN ('Pending for Verification', 'Verified') THEN 1 ELSE 0 END) AS CountPending,
                        SUM(CASE WHEN m.ApplicationStatus = 'Approved' AND ms.MembershipStatus != 'Cancelled' THEN 1 ELSE 0 END) AS CountApproved,
                        SUM(CASE WHEN m.ApplicationStatus LIKE 'Rejected%' OR ms.MembershipStatus = 'Cancelled' THEN 1 ELSE 0 END) AS CountRejected
                    FROM dbo.Members m
                    LEFT JOIN dbo.Memberships ms ON m.MemberId = ms.MemberId
                    LEFT JOIN dbo.MembershipTerms mt ON ms.MembershipId = mt.MembershipId AND mt.MembershipYear = @Year
                    {directoryFilterSql};";

                var counts = db.QuerySingle<dynamic>(countSql, new { SearchTerm = searchParam, Year = year });

                // 2. Tab Filter
                string filterClause = "";
                if (tabParam == "Pending")
                    filterClause = " AND m.ApplicationStatus IN ('Pending for Verification', 'Verified') ";
                else if (tabParam == "Approved")
                    filterClause = " AND m.ApplicationStatus = 'Approved' AND ms.MembershipStatus != 'Cancelled' ";
                else if (tabParam == "Rejected")
                    filterClause = " AND (m.ApplicationStatus LIKE 'Rejected%' OR ms.MembershipStatus = 'Cancelled') ";

                // 3. Total Records
                string totalSql = $@"
                    SELECT COUNT(1) FROM dbo.Members m
                    LEFT JOIN dbo.Memberships ms ON m.MemberId = ms.MemberId
                    LEFT JOIN dbo.MembershipTerms mt ON ms.MembershipId = mt.MembershipId AND mt.MembershipYear = @Year
                    {directoryFilterSql}
                    {filterClause};";

                int totalRecords = db.ExecuteScalar<int>(totalSql, new { SearchTerm = searchParam, Year = year });

                // 4. Paged Data
                string dataSql = $@"
                    SELECT 
                        m.*,
                        ms.MembershipId,
                        ms.MembershipNumber,
                        ms.MembershipStatus,
                        ms.CancellationReason,
                        mt.TermStatus AS CurrentTermStatus
                    FROM dbo.Members m
                    LEFT JOIN dbo.Memberships ms ON m.MemberId = ms.MemberId
                    LEFT JOIN dbo.MembershipTerms mt ON ms.MembershipId = mt.MembershipId 
                        AND mt.MembershipYear = @Year
                    {directoryFilterSql}
                    {filterClause}
                    ORDER BY m.ApplicationDate DESC, m.MemberId DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                var members = db.Query<Member>(dataSql, new
                {
                    SearchTerm = searchParam,
                    Year = year,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                }).ToList();

                // 5. Latest Payments for page (For Print Receipt button)
                var paymentsDict = new Dictionary<int, Payment>();
                var memberIds = members.Select(m => m.MemberId).ToList();

                if (memberIds.Any())
                {
                    const string paySql = @"
                        SELECT p.*, mt.MembershipYear, ms.MemberId AS MemberId
                        FROM dbo.Payments p
                        INNER JOIN dbo.MembershipTerms mt ON p.MembershipTermId = mt.MembershipTermId
                        INNER JOIN dbo.Memberships ms ON mt.MembershipId = ms.MembershipId
                        WHERE ms.MemberId IN @MemberIds AND mt.MembershipYear = @Year
                        ORDER BY p.PaymentDate DESC;";

                    var allPayments = db.Query<Payment>(paySql, new { MemberIds = memberIds, Year = year }).ToList();

                    foreach (var group in allPayments.GroupBy(p => p.MemberId))
                        paymentsDict[group.Key] = group.First();
                }

                // 6. Return ViewModel
                return new DirectoryViewModel
                {
                    Members = members,
                    LatestPayments = paymentsDict,
                    SearchTerm = searchTerm,
                    CurrentTab = tabParam,
                    SelectedYear = year,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    TotalRecords = totalRecords,
                    CountAll = (int)(counts.CountAll ?? 0),
                    CountPending = (int)(counts.CountPending ?? 0),
                    CountApproved = (int)(counts.CountApproved ?? 0),
                    CountRejected = (int)(counts.CountRejected ?? 0)
                };
            }
        }

        #endregion

        private ApplicationListViewModel GetPagedApplications(
            IDbConnection db,
            string countSql,
            string dataSql,
            object parameters,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            // 1. Count all matching applications.
            int totalRecords = db.ExecuteScalar<int>(
                countSql,
                parameters);

            // 2. Add pagination parameters to the existing query parameters.
            var dataParameters = new DynamicParameters(parameters);

            dataParameters.Add("Offset", (page - 1) * pageSize);
            dataParameters.Add("PageSize", pageSize);

            // 3. Retrieve only the records for the selected page.
            var members = db.Query<Member>(
                dataSql,
                dataParameters).ToList();

            // 4. Calculate the total number of pages.
            int totalPages = (int)Math.Ceiling(
                (double)totalRecords / pageSize);

            // 5. Return the paginated result.
            return new ApplicationListViewModel
            {
                Members = members,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                PageSize = pageSize
            };
        }
    }
}