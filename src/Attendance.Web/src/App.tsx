import { FormEvent, ReactNode, useEffect, useState } from "react";
import ExpensePolicy from "./ExpensePolicy";

const API = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

type Organization = { id: string; organizationCode: string; organizationName: string; legalName?: string; timeZoneId?: string; isActive: boolean };
type Branch = { id: string; organizationId: string; branchCode: string; branchName: string; address?: string; isActive: boolean };
type Department = { id: string; organizationId: string; departmentCode: string; departmentName: string; isActive: boolean };
type LeaveType = { id:string; leaveCode:string; leaveName:string; description?:string; isPaid:boolean; isHalfDayAllowed:boolean; requiresAttachment:boolean; isActive:boolean };
type LeavePolicy = { id:string; leaveTypeId:string; policyName:string; accrualType:string; monthlyEntitlement:number; annualEntitlement?:number; carryForwardAllowed:boolean; maximumCarryForward:number; allowNegativeBalance:boolean; isActive:boolean; effectiveFrom:string; effectiveTo?:string };
type LeaveBalance = { id:string; employeeId:string; leavePolicyId:string; leaveTypeId:string; leaveCode:string; leaveName:string; policyName:string; accrualType:string; monthlyEntitlement:number; annualEntitlement?:number; balanceYear:number; balanceMonth:number; entitledDays:number; adjustmentDays:number; usedDays:number; expiredDays:number; availableDays:number };
type EmployeeGrade = { id:string; gradeCode:string; gradeName:string; description?:string; isActive:boolean };
type Employee = {
  id: string; employeeCode: string; fullName: string;\n  gradeId?: string;
  dateOfBirth?: string; gender?: string; mobileNumber?: string; emailAddress?: string; address?: string;
  departmentId?: string; branchId?: string; reportingManagerId?: string;
  designation?: string; employmentType?: string; joiningDate: string; confirmationDate?: string;
  biometricUserId?: string; emergencyContactName?: string; emergencyContactNumber?: string; emergencyContactRelation?: string;
  profilePhotoFileName?: string; profilePhotoUpdatedAtUtc?: string; isActive: boolean;
};

async function api<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(API + path, { headers: { "Content-Type": "application/json", ...(options?.headers ?? {}) }, ...options });
  if (!response.ok) throw new Error((await response.text()) || response.statusText);
  return response.status === 204 ? (undefined as T) : response.json();
}

function App() {
  const [tab, setTab] = useState("Dashboard");
  const [organizations, setOrganizations] = useState<Organization[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);\n  const [employeeGrades, setEmployeeGrades] = useState<EmployeeGrade[]>([]);
  const [error, setError] = useState("");
  const [bulkFile, setBulkFile] = useState<File | null>(null);
  const [bulkMessage, setBulkMessage] = useState("");
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(null);
  const [leaveTypes, setLeaveTypes] = useState<LeaveType[]>([]);
  const [leaveBalances, setLeaveBalances] = useState<LeaveBalance[]>([]);
  const [balanceEmployeeId, setBalanceEmployeeId] = useState("");
  const [balanceYear, setBalanceYear] = useState(String(new Date().getFullYear()));
  const [balanceMessage, setBalanceMessage] = useState("");
  const [balanceBusy, setBalanceBusy] = useState(false);
  const [leavePolicies, setLeavePolicies] = useState<LeavePolicy[]>([]);
  const [leaveCode, setLeaveCode] = useState(""); const [leaveName, setLeaveName] = useState("");
  const [leavePaid, setLeavePaid] = useState(true); const [leaveHalfDay, setLeaveHalfDay] = useState(true); const [leaveAttachment, setLeaveAttachment] = useState(false);
  const [policyLeaveTypeId, setPolicyLeaveTypeId] = useState(""); const [policyName, setPolicyName] = useState("");
  const [policyAccrual, setPolicyAccrual] = useState("Monthly"); const [policyMonthly, setPolicyMonthly] = useState("1"); const [policyAnnual, setPolicyAnnual] = useState(""); const [policyEffectiveFrom, setPolicyEffectiveFrom] = useState(new Date().toISOString().slice(0,10));

  const load = async () => {
    try {
      setError("");
      const [o,b,d,e,g,lt,lp] = await Promise.all([
        api<Organization[]>("/api/organizations"),
        api<Branch[]>("/api/branches"),
        api<Department[]>("/api/departments"),
        api<Employee[]>("/api/employees"),
        api<LeaveType[]>("/api/leave-types"),
        api<LeavePolicy[]>("/api/leave-policies")
      ]);
      setOrganizations(o); setBranches(b); setDepartments(d); setEmployees(e); setEmployeeGrades(g); setLeaveTypes(lt); setLeavePolicies(lp);
    } catch (e) { setError(e instanceof Error ? e.message : "API connection failed"); }
  };

  useEffect(() => { void load(); }, []);

  const loadLeaveBalances = async (employeeId = balanceEmployeeId) => {
    if (!employeeId) { setLeaveBalances([]); return; }
    try {
      setBalanceMessage("");
      const rows = await api<LeaveBalance[]>(`/api/leave-balances/employee/${employeeId}?year=${balanceYear}`);
      setLeaveBalances(rows);
    } catch(e) { setError(e instanceof Error ? e.message : "Could not load leave balances"); }
  };

  const accrueLeaveBalances = async () => {
    if (!balanceEmployeeId) { setError("Select an employee first."); return; }
    try {
      setBalanceBusy(true); setError(""); setBalanceMessage("");
      const month = new Date().getMonth() + 1;
      const year = new Date().getFullYear();
      const result = await api<{message:string}>(`/api/leave-balances/accrue?year=${year}&month=${month}`, {method:"POST"});
      setBalanceMessage(result.message);
      setBalanceYear(String(year));
      await loadLeaveBalances(balanceEmployeeId);
    } catch(e) { setError(e instanceof Error ? e.message : "Could not accrue leave balances"); }
    finally { setBalanceBusy(false); }
  };


  const createOrganization = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const f = new FormData(form);
    try {
      await api("/api/organizations", { method: "POST", body: JSON.stringify({
        organizationCode: f.get("code"), organizationName: f.get("name"), legalName: f.get("legalName") || null,
        timeZoneId: "Asia/Kolkata", isActive: true
      })});
      form.reset(); await load();
    } catch(e) { setError(e instanceof Error ? e.message : "Could not create organization"); }
  };

  const createBranch = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const form = event.currentTarget; const f = new FormData(form);
    try {
      await api("/api/branches", { method:"POST", body: JSON.stringify({
        organizationId:f.get("organizationId"), branchCode:f.get("code"), branchName:f.get("name"), address:f.get("address") || null, isActive:true
      })}); form.reset(); await load();
    } catch(e) { setError(e instanceof Error ? e.message : "Could not create branch"); }
  };

  const createDepartment = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const form = event.currentTarget; const f = new FormData(form);
    try {
      await api("/api/departments", { method:"POST", body: JSON.stringify({
        organizationId:f.get("organizationId"), departmentCode:f.get("code"), departmentName:f.get("name"), isActive:true
      })}); form.reset(); await load();
    } catch(e) { setError(e instanceof Error ? e.message : "Could not create department"); }
  };

  const createEmployee = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const form = event.currentTarget; const f = new FormData(form);
    try {
      await api("/api/employees", { method:"POST", body: JSON.stringify({
        employeeCode:f.get("code"), fullName:f.get("name"), branchId:f.get("branchId") || null,
        departmentId:f.get("departmentId") || null, reportingManagerId:f.get("managerId") || null,
        joiningDate:f.get("joiningDate"), gradeId:f.get("gradeId") || null, isActive:true
      })}); form.reset(); await load();
    } catch(e) { setError(e instanceof Error ? e.message : "Could not create employee"); }
  };

  const downloadEmployeeTemplate = () => {
    const csv = "OrganizationCode,EmployeeCode,FullName,BranchCode,DepartmentCode,ReportingManagerCode,JoiningDate,IsActive\nACPL,00207,Ravi,PATTANUR,SS,00206,2026-09-01,true\n";
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = "AnujHRMS_Employee_Template.csv";
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const uploadEmployees = async () => {
    if (!bulkFile) {
      setError("Please select an employee CSV file.");
      return;
    }

    try {
      setError("");
      setBulkMessage("");
      const form = new FormData();
      form.append("file", bulkFile);

      const response = await fetch(API + "/api/employees/bulk-upload", {
        method: "POST",
        body: form
      });

      const body = await response.json();
      if (!response.ok) {
        const details = body.errors?.map((x: { row: number; employeeCode: string; errors: string[] }) =>
          `Row ${x.row} (${x.employeeCode || "blank code"}): ${x.errors.join(" ")}`
        ).join(" | ");
        throw new Error(details ? `${body.message} ${details}` : (body.message ?? "Bulk upload failed."));
      }

      setBulkMessage(`${body.message} Imported: ${body.imported} / ${body.totalRows} rows.`);
      setBulkFile(null);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Bulk upload failed.");
    }
  };

  const updateEmployee = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedEmployee) return;
    const form = event.currentTarget;
    const f = new FormData(form);
    try {
      setError("");
      const updated = await api<Employee>(`/api/employees/${selectedEmployee.id}`, {
        method: "PUT",
        body: JSON.stringify({
          id: selectedEmployee.id,
          employeeCode: f.get("employeeCode"),
          fullName: f.get("fullName"),
          dateOfBirth: f.get("dateOfBirth") || null,
          gender: f.get("gender") || null,
          mobileNumber: f.get("mobileNumber") || null,
          emailAddress: f.get("emailAddress") || null,
          address: f.get("address") || null,
          emergencyContactName: f.get("emergencyContactName") || null,
          emergencyContactNumber: f.get("emergencyContactNumber") || null,
          emergencyContactRelation: f.get("emergencyContactRelation") || null,
          designation: f.get("designation") || null,
          employmentType: f.get("employmentType") || null,
          branchId: f.get("branchId") || null,
          departmentId: f.get("departmentId") || null,
          reportingManagerId: f.get("reportingManagerId") || null,
          joiningDate: f.get("joiningDate"),
          confirmationDate: f.get("confirmationDate") || null,
          biometricUserId: f.get("biometricUserId") || null,
          isActive: f.get("isActive") === "true"
        })
      });
      setSelectedEmployee(updated ?? null);
      await load();
      setBulkMessage("Employee details saved.");
    } catch(e) {
      setError(e instanceof Error ? e.message : "Could not save employee details");
    }
  };

  const createLeaveType = async (event: FormEvent) => {
    event.preventDefault();
    try {
      await api("/api/leave-types",{method:"POST",body:JSON.stringify({leaveCode:leaveCode.trim(),leaveName:leaveName.trim(),isPaid:leavePaid,isHalfDayAllowed:leaveHalfDay,requiresAttachment:leaveAttachment,isActive:true})});
      setLeaveCode(""); setLeaveName(""); setLeavePaid(true); setLeaveHalfDay(true); setLeaveAttachment(false); await load();
    } catch(e){setError(e instanceof Error?e.message:"Could not create leave type");}
  };
  const createLeavePolicy = async (event: FormEvent) => {
    event.preventDefault();
    try {
      await api("/api/leave-policies",{method:"POST",body:JSON.stringify({leaveTypeId:policyLeaveTypeId,policyName:policyName.trim(),accrualType:policyAccrual,monthlyEntitlement:policyAccrual==="Monthly"?Number(policyMonthly):0,annualEntitlement:policyAccrual==="Annual"?Number(policyAnnual):null,carryForwardAllowed:false,maximumCarryForward:0,allowNegativeBalance:false,isActive:true,effectiveFrom:policyEffectiveFrom})});
      setPolicyName(""); setPolicyMonthly("1"); setPolicyAnnual(""); await load();
    } catch(e){setError(e instanceof Error?e.message:"Could not create leave policy");}
  };

  const nav = ["Dashboard","Organizations","Branches","Departments","Employees","Leave","Leave Balance","Expense Policy"];
  return <div className="app">
    <aside className="sidebar">
      <div className="brand"><div className="brand-mark">A</div><div><strong>AnujHRMS</strong><span>Human Resource Management</span></div></div>
      <nav>{nav.map(x => <button className={tab===x?"active":""} onClick={()=>setTab(x)} key={x}>{x}</button>)}</nav>
      <div className="sidebar-foot">Phase 1 · Master Data</div>
    </aside>
    <main>
      <header><div><small>ANUJ HRMS</small><h1>{tab}</h1></div><button className="refresh" onClick={()=>void load()}>↻ Refresh</button></header>
      {error && <div className="error">{error}</div>}
      {tab==="Dashboard" && <Dashboard organizations={organizations} branches={branches} departments={departments} employees={employees}/>}
      {tab==="Organizations" && <Section title="Organizations" form={createOrganization}><input name="code" placeholder="Organization code" required/><input name="name" placeholder="Organization name" required/><input name="legalName" placeholder="Legal name"/><button type="submit">Add Organization</button><List rows={organizations.map(x=>[x.organizationCode,x.organizationName,x.isActive?"Active":"Inactive"])}/></Section>}
      {tab==="Branches" && <Section title="Branches" form={createBranch}><Select name="organizationId" placeholder="Organization" items={organizations.map(x=>({id:x.id,label:x.organizationName}))}/><input name="code" placeholder="Branch code" required/><input name="name" placeholder="Branch name" required/><input name="address" placeholder="Address"/><button type="submit">Add Branch</button><List rows={branches.map(x=>[x.branchCode,x.branchName,organizations.find(o=>o.id===x.organizationId)?.organizationName??"-"])}/></Section>}
      {tab==="Departments" && <Section title="Departments" form={createDepartment}><Select name="organizationId" placeholder="Organization" items={organizations.map(x=>({id:x.id,label:x.organizationName}))}/><input name="code" placeholder="Department code" required/><input name="name" placeholder="Department name" required/><button type="submit">Add Department</button><List rows={departments.map(x=>[x.departmentCode,x.departmentName,organizations.find(o=>o.id===x.organizationId)?.organizationName??"-"])}/></Section>}
      {tab==="Employees" && <Section title="Employees" form={createEmployee}><input name="code" placeholder="Employee code" required/><input name="name" placeholder="Full name" required/><Select name="branchId" placeholder="Branch (optional)" optional items={branches.map(x=>({id:x.id,label:x.branchName}))}/><Select name="departmentId" placeholder="Department (optional)" optional items={departments.map(x=>({id:x.id,label:x.departmentName}))}/><Select name="managerId" placeholder="Reporting manager (optional)" optional items={employees.map(x=>({id:x.id,label:x.employeeCode+" · "+x.fullName}))}/><Select name="gradeId" placeholder="Employee grade (optional)" optional items={employeeGrades.map(x=>({id:x.id,label:x.gradeCode+" · "+x.gradeName}))}/><input type="date" name="joiningDate" required/><button type="submit">Add Employee</button><div className="bulk-upload"><div><strong>Bulk Employee Upload</strong><span>CSV only · all rows are validated before import</span></div><input type="file" accept=".csv,text/csv" onChange={e=>setBulkFile(e.target.files?.[0] ?? null)}/><button type="button" onClick={downloadEmployeeTemplate}>Download Template</button><button type="button" onClick={()=>void uploadEmployees()}>Upload CSV</button>{bulkMessage && <div className="success">{bulkMessage}</div>}</div><div className="employee-table table-wrap"><table><thead><tr><th>Code</th><th>Name</th><th>Branch</th><th>Department</th><th>Status</th><th></th></tr></thead><tbody>{employees.length===0?<tr><td colSpan={6} className="empty">No records yet.</td></tr>:employees.map(x=><tr key={x.id}><td>{x.employeeCode}</td><td>{x.fullName}</td><td>{branches.find(b=>b.id===x.branchId)?.branchName??"-"}</td><td>{departments.find(d=>d.id===x.departmentId)?.departmentName??"-"}</td><td>{x.isActive?"Active":"Inactive"}</td><td><button type="button" className="link-button" onClick={()=>setSelectedEmployee(x)}>View / Edit</button></td></tr>)}</tbody></table></div></Section>}
      {tab==="Leave" && <LeaveManagement leaveTypes={leaveTypes} leavePolicies={leavePolicies} leaveCode={leaveCode} setLeaveCode={setLeaveCode} leaveName={leaveName} setLeaveName={setLeaveName} leavePaid={leavePaid} setLeavePaid={setLeavePaid} leaveHalfDay={leaveHalfDay} setLeaveHalfDay={setLeaveHalfDay} leaveAttachment={leaveAttachment} setLeaveAttachment={setLeaveAttachment} policyLeaveTypeId={policyLeaveTypeId} setPolicyLeaveTypeId={setPolicyLeaveTypeId} policyName={policyName} setPolicyName={setPolicyName} policyAccrual={policyAccrual} setPolicyAccrual={setPolicyAccrual} policyMonthly={policyMonthly} setPolicyMonthly={setPolicyMonthly} policyAnnual={policyAnnual} setPolicyAnnual={setPolicyAnnual} policyEffectiveFrom={policyEffectiveFrom} setPolicyEffectiveFrom={setPolicyEffectiveFrom} onCreateType={createLeaveType} onCreatePolicy={createLeavePolicy}/>}
      {tab==="Expense Policy" && <ExpensePolicy/>}
      {tab==="Leave Balance" && <LeaveBalanceManagement employees={employees} balances={leaveBalances} employeeId={balanceEmployeeId} setEmployeeId={setBalanceEmployeeId} year={balanceYear} setYear={setBalanceYear} onLoad={()=>void loadLeaveBalances()} onAccrue={()=>void accrueLeaveBalances()} busy={balanceBusy} message={balanceMessage}/>}
      {selectedEmployee && <EmployeeDetails employee={selectedEmployee} branches={branches} departments={departments} employees={employees} employeeGrades={employeeGrades} onClose={()=>setSelectedEmployee(null)} onSubmit={updateEmployee}/>}

    </main>
  </div>;
}

function EmployeeDetails({employee,branches,departments,employees,employeeGrades,onClose,onSubmit}:{employee:Employee;branches:Branch[];departments:Department[];employees:Employee[];employeeGrades:EmployeeGrade[];onClose:()=>void;onSubmit:(e:FormEvent<HTMLFormElement>)=>void}) {
  const [photoVersion,setPhotoVersion]=useState(Date.now());
  const [photoFile,setPhotoFile]=useState<File | null>(null);
  const [photoBusy,setPhotoBusy]=useState(false);
  const [photoMessage,setPhotoMessage]=useState("");
  const [photoError,setPhotoError]=useState("");
  const photoUrl=employee.profilePhotoFileName ? API + "/api/employees/" + employee.id + "/photo?v=" + photoVersion : "";
  const initials=employee.fullName.split(/\s+/).filter(Boolean).slice(0,2).map(x=>x[0]).join("").toUpperCase();

  const uploadPhoto=async()=>{
    if(!photoFile) return;
    try{
      setPhotoBusy(true); setPhotoError(""); setPhotoMessage("");
      const form=new FormData(); form.append("file",photoFile);
      const response=await fetch(API + "/api/employees/" + employee.id + "/photo",{method:"POST",body:form});
      const body=await response.json().catch(()=>null);
      if(!response.ok) throw new Error(body?.message ?? body ?? "Photo upload failed.");
      setPhotoFile(null); setPhotoVersion(Date.now()); setPhotoMessage("Profile photo updated.");
      const input=document.getElementById("employee-photo-input-" + employee.id) as HTMLInputElement | null;
      if(input) input.value="";
    }catch(e){setPhotoError(e instanceof Error ? e.message : "Photo upload failed.");}
    finally{setPhotoBusy(false);}
  };

  const removePhoto=async()=>{
    if(!employee.profilePhotoFileName) return;
    if(!window.confirm("Remove this employee profile photo?")) return;
    try{
      setPhotoBusy(true); setPhotoError(""); setPhotoMessage("");
      const response=await fetch(API + "/api/employees/" + employee.id + "/photo",{method:"DELETE"});
      if(!response.ok) throw new Error((await response.text()) || "Could not remove photo.");
      setPhotoVersion(Date.now()); setPhotoMessage("Profile photo removed.");
    }catch(e){setPhotoError(e instanceof Error ? e.message : "Could not remove photo.");}
    finally{setPhotoBusy(false);}
  };

  return <div className="details-overlay"><div className="details-panel">
    <div className="details-head"><div><small>EMPLOYEE MASTER</small><h2>{employee.employeeCode} · {employee.fullName}</h2></div><button type="button" className="close-button" onClick={onClose}>Close</button></div>
    <div className="employee-photo-card">
      <div className="employee-photo-preview">{photoUrl ? <img src={photoUrl} alt={employee.fullName + " profile"} /> : <span>{initials || "E"}</span>}</div>
      <div className="employee-photo-info"><strong>Profile Photo</strong><span>JPG, JPEG or PNG · maximum 5 MB</span>
        <div className="employee-photo-actions"><label className="photo-select-button">{photoFile ? photoFile.name : "Choose Photo"}<input id={"employee-photo-input-" + employee.id} type="file" accept="image/jpeg,image/png,.jpg,.jpeg,.png" onChange={e=>{setPhotoFile(e.target.files?.[0] ?? null);setPhotoMessage("");setPhotoError("");}} /></label>
        <button type="button" onClick={()=>void uploadPhoto()} disabled={!photoFile || photoBusy}>{photoBusy ? "Uploading..." : "Upload Photo"}</button>
        {employee.profilePhotoFileName && <button type="button" className="close-button" onClick={()=>void removePhoto()} disabled={photoBusy}>Remove</button>}</div>
        {photoMessage && <div className="success photo-status">{photoMessage}</div>}{photoError && <div className="photo-error">{photoError}</div>}
      </div>
    </div>
    <form onSubmit={onSubmit} className="details-form">
      <h3>Personal Details</h3>
      <input name="employeeCode" defaultValue={employee.employeeCode} placeholder="Employee code" required/>
      <input name="fullName" defaultValue={employee.fullName} placeholder="Full name" required/>
      <input type="date" name="dateOfBirth" defaultValue={employee.dateOfBirth?.slice(0,10) ?? ""}/>
      <select name="gender" defaultValue={employee.gender ?? ""}><option value="">Gender</option><option>Male</option><option>Female</option><option>Other</option></select>
      <input name="mobileNumber" defaultValue={employee.mobileNumber ?? ""} placeholder="Mobile number"/>
      <input type="email" name="emailAddress" defaultValue={employee.emailAddress ?? ""} placeholder="Email address"/>
      <textarea name="address" defaultValue={employee.address ?? ""} placeholder="Address"/>
      <h3>Emergency Contact</h3>
      <input name="emergencyContactName" defaultValue={employee.emergencyContactName ?? ""} placeholder="Contact name"/>
      <input name="emergencyContactNumber" defaultValue={employee.emergencyContactNumber ?? ""} placeholder="Contact number"/>
      <input name="emergencyContactRelation" defaultValue={employee.emergencyContactRelation ?? ""} placeholder="Relationship"/>
      <h3>Employment Details</h3>
      <input name="designation" defaultValue={employee.designation ?? ""} placeholder="Designation"/>
      <select name="employmentType" defaultValue={employee.employmentType ?? ""}><option value="">Employment type</option><option>Permanent</option><option>Probation</option><option>Contract</option><option>Temporary</option><option>Intern</option></select>
      <Select name="branchId" placeholder="Branch" optional defaultValue={employee.branchId} items={branches.map(x=>({id:x.id,label:x.branchName}))}/>
      <Select name="departmentId" placeholder="Department" optional defaultValue={employee.departmentId} items={departments.map(x=>({id:x.id,label:x.departmentName}))}/>
      <Select name="reportingManagerId" placeholder="Reporting manager" optional defaultValue={employee.reportingManagerId} items={employees.filter(x=>x.id!==employee.id).map(x=>({id:x.id,label:x.employeeCode+" · "+x.fullName}))}/>
      <input type="date" name="joiningDate" defaultValue={employee.joiningDate?.slice(0,10) ?? ""} required/>
      <input type="date" name="confirmationDate" defaultValue={employee.confirmationDate?.slice(0,10) ?? ""}/>
      <input name="biometricUserId" defaultValue={employee.biometricUserId ?? ""} placeholder="Biometric / device user ID"/>
      <select name="isActive" defaultValue={employee.isActive ? "true" : "false"}><option value="true">Active</option><option value="false">Inactive</option></select>
      <div className="details-actions"><button type="button" className="close-button" onClick={onClose}>Cancel</button><button type="submit">Save Employee Details</button></div>
    </form>
  </div></div>;
}

function LeaveBalanceManagement(p:{employees:Employee[];balances:LeaveBalance[];employeeId:string;setEmployeeId:(v:string)=>void;year:string;setYear:(v:string)=>void;onLoad:()=>void;onAccrue:()=>void;busy:boolean;message:string}) {
  const months = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];
  return <div className="content">
    <div className="panel leave-balance-toolbar">
      <div><h2>Employee Leave Balance</h2><p className="muted">View monthly entitlement, usage and available leave for an employee.</p></div>
      <div className="balance-controls">
        <select value={p.employeeId} onChange={e=>p.setEmployeeId(e.target.value)}><option value="">Select employee</option>{p.employees.map(x=><option value={x.id} key={x.id}>{x.employeeCode} · {x.fullName}</option>)}</select>
        <input type="number" value={p.year} onChange={e=>p.setYear(e.target.value)} min="2020" max="2100"/>
        <button type="button" onClick={p.onLoad} disabled={!p.employeeId}>Load Balance</button>
        <button type="button" className="secondary-button" onClick={p.onAccrue} disabled={!p.employeeId || p.busy}>{p.busy ? "Accruing..." : "Accrue Current Month"}</button>
      </div>
      {p.message && <div className="success">{p.message}</div>}
    </div>
    <div className="panel">
      <div className="table-wrap">
        <table><thead><tr><th>Month</th><th>Leave</th><th>Policy</th><th>Entitled</th><th>Used</th><th>Adjusted</th><th>Expired</th><th>Available</th></tr></thead>
        <tbody>{p.balances.length===0?<tr><td colSpan={8} className="empty">No balance records for this employee and year. Run the accrual process for the current month.</td></tr>:p.balances.map(x=><tr key={x.id}><td>{months[x.balanceMonth-1]} {x.balanceYear}</td><td><strong>{x.leaveCode}</strong> · {x.leaveName}</td><td>{x.policyName}</td><td>{x.entitledDays.toFixed(2)}</td><td>{x.usedDays.toFixed(2)}</td><td>{x.adjustmentDays.toFixed(2)}</td><td>{x.expiredDays.toFixed(2)}</td><td><strong>{x.availableDays.toFixed(2)}</strong></td></tr>)}</tbody></table>
      </div>
    </div>
  </div>;
}

function LeaveManagement(p:{
  leaveTypes:LeaveType[]; leavePolicies:LeavePolicy[];
  leaveCode:string;setLeaveCode:(v:string)=>void;leaveName:string;setLeaveName:(v:string)=>void;leavePaid:boolean;setLeavePaid:(v:boolean)=>void;leaveHalfDay:boolean;setLeaveHalfDay:(v:boolean)=>void;leaveAttachment:boolean;setLeaveAttachment:(v:boolean)=>void;
  policyLeaveTypeId:string;setPolicyLeaveTypeId:(v:string)=>void;policyName:string;setPolicyName:(v:string)=>void;policyAccrual:string;setPolicyAccrual:(v:string)=>void;policyMonthly:string;setPolicyMonthly:(v:string)=>void;policyAnnual:string;setPolicyAnnual:(v:string)=>void;policyEffectiveFrom:string;setPolicyEffectiveFrom:(v:string)=>void;
  onCreateType:(e:FormEvent)=>void;onCreatePolicy:(e:FormEvent)=>void;
}) {
  return <div className="content">
    <div className="panel"><h2>Leave Types</h2><form onSubmit={p.onCreateType}>
      <input value={p.leaveCode} onChange={e=>p.setLeaveCode(e.target.value)} placeholder="Leave code" required/>
      <input value={p.leaveName} onChange={e=>p.setLeaveName(e.target.value)} placeholder="Leave name" required/>
      <label className="check-field"><input type="checkbox" checked={p.leavePaid} onChange={e=>p.setLeavePaid(e.target.checked)}/> Paid leave</label>
      <label className="check-field"><input type="checkbox" checked={p.leaveHalfDay} onChange={e=>p.setLeaveHalfDay(e.target.checked)}/> Half-day allowed</label>
      <label className="check-field"><input type="checkbox" checked={p.leaveAttachment} onChange={e=>p.setLeaveAttachment(e.target.checked)}/> Attachment required</label>
      <button type="submit">Add Leave Type</button>
    </form><div className="table-wrap"><table><thead><tr><th>Code</th><th>Name</th><th>Paid</th><th>Half Day</th><th>Attachment</th><th>Status</th></tr></thead><tbody>{p.leaveTypes.length===0?<tr><td colSpan={6} className="empty">No leave types configured.</td></tr>:p.leaveTypes.map(x=><tr key={x.id}><td>{x.leaveCode}</td><td>{x.leaveName}</td><td>{x.isPaid?"Yes":"No"}</td><td>{x.isHalfDayAllowed?"Yes":"No"}</td><td>{x.requiresAttachment?"Yes":"No"}</td><td>{x.isActive?"Active":"Inactive"}</td></tr>)}</tbody></table></div></div>
    <div className="panel"><h2>Leave Policies</h2><form onSubmit={p.onCreatePolicy}>
      <select value={p.policyLeaveTypeId} onChange={e=>p.setPolicyLeaveTypeId(e.target.value)} required><option value="">Leave type</option>{p.leaveTypes.map(x=><option value={x.id} key={x.id}>{x.leaveCode} · {x.leaveName}</option>)}</select>
      <input value={p.policyName} onChange={e=>p.setPolicyName(e.target.value)} placeholder="Policy name" required/>
      <select value={p.policyAccrual} onChange={e=>p.setPolicyAccrual(e.target.value)}><option value="Monthly">Monthly</option><option value="Annual">Annual</option><option value="None">None</option></select>
      {p.policyAccrual==="Monthly" && <input type="number" min="0.01" step="0.01" value={p.policyMonthly} onChange={e=>p.setPolicyMonthly(e.target.value)} placeholder="Days per month" required/>}
      {p.policyAccrual==="Annual" && <input type="number" min="0.01" step="0.01" value={p.policyAnnual} onChange={e=>p.setPolicyAnnual(e.target.value)} placeholder="Days per year" required/>}
      <input type="date" value={p.policyEffectiveFrom} onChange={e=>p.setPolicyEffectiveFrom(e.target.value)} required/>
      <div className="policy-rule"><strong>Carry Forward: No</strong><span>Unused monthly balance expires at month end.</span></div>
      <button type="submit">Add Leave Policy</button>
    </form><div className="table-wrap"><table><thead><tr><th>Policy</th><th>Leave Type</th><th>Accrual</th><th>Entitlement</th><th>Carry Forward</th><th>Effective</th></tr></thead><tbody>{p.leavePolicies.length===0?<tr><td colSpan={6} className="empty">No leave policies configured.</td></tr>:p.leavePolicies.map(x=><tr key={x.id}><td>{x.policyName}</td><td>{p.leaveTypes.find(t=>t.id===x.leaveTypeId)?.leaveName??"Unknown"}</td><td>{x.accrualType}</td><td>{x.accrualType==="Monthly"?x.monthlyEntitlement+" / month":x.accrualType==="Annual"?x.annualEntitlement+" / year":"None"}</td><td>{x.carryForwardAllowed?"Yes":"No"}</td><td>{x.effectiveFrom}</td></tr>)}</tbody></table></div></div>
  </div>;
}
function Dashboard({organizations,branches,departments,employees}:{organizations:Organization[];branches:Branch[];departments:Department[];employees:Employee[]}) {
  return <><div className="welcome"><div><small>WELCOME</small><h2>HRMS foundation is ready.</h2><p>Set up your organization structure and employee master before connecting biometric attendance.</p></div></div><div className="cards">{[["Organizations",organizations.length],["Branches",branches.length],["Departments",departments.length],["Active Employees",employees.length]].map(([a,b])=><div className="card" key={String(a)}><span>{a}</span><strong>{b}</strong></div>)}</div><div className="panel"><h3>Implementation path</h3><div className="steps"><span>01 Master Data</span><span>02 Users & Roles</span><span>03 Devices & Punches</span><span>04 Attendance Engine</span><span>05 Leave Workflow</span></div></div></>;
}
function Section({title,form,children}:{title:string;form:(e:FormEvent<HTMLFormElement>)=>void;children:ReactNode}) {
  return <div className="content"><div className="panel"><h2>Add {title.slice(0,-1)}</h2><form onSubmit={form}>{children}</form></div></div>;
}
function Select({name,placeholder,items,optional,defaultValue}:{name:string;placeholder:string;items:{id:string;label:string}[];optional?:boolean;defaultValue?:string}) {
  return <select name={name} required={!optional} defaultValue={defaultValue ?? ""}><option value="">{placeholder}</option>{items.map(x=><option value={x.id} key={x.id}>{x.label}</option>)}</select>;
}
function List({rows}:{rows:string[][]}) {
  return <div className="table-wrap"><table><tbody>{rows.length===0?<tr><td className="empty">No records yet.</td></tr>:rows.map((r,i)=><tr key={i}>{r.map((c,j)=><td key={j}>{c}</td>)}</tr>)}</tbody></table></div>;
}
export default App;
