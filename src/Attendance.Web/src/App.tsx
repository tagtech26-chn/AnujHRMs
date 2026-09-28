import { FormEvent, ReactNode, useEffect, useState } from "react";

const API = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

type Organization = { id: string; organizationCode: string; organizationName: string; legalName?: string; timeZoneId?: string; isActive: boolean };
type Branch = { id: string; organizationId: string; branchCode: string; branchName: string; address?: string; isActive: boolean };
type Department = { id: string; organizationId: string; departmentCode: string; departmentName: string; isActive: boolean };
type Employee = {
  id: string; employeeCode: string; fullName: string;
  dateOfBirth?: string; gender?: string; mobileNumber?: string; emailAddress?: string; address?: string;
  departmentId?: string; branchId?: string; reportingManagerId?: string;
  designation?: string; employmentType?: string; joiningDate: string; confirmationDate?: string;
  biometricUserId?: string; isActive: boolean;
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
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [error, setError] = useState("");
  const [bulkFile, setBulkFile] = useState<File | null>(null);
  const [bulkMessage, setBulkMessage] = useState("");
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(null);

  const load = async () => {
    try {
      setError("");
      const [o,b,d,e] = await Promise.all([
        api<Organization[]>("/api/organizations"),
        api<Branch[]>("/api/branches"),
        api<Department[]>("/api/departments"),
        api<Employee[]>("/api/employees")
      ]);
      setOrganizations(o); setBranches(b); setDepartments(d); setEmployees(e);
    } catch (e) { setError(e instanceof Error ? e.message : "API connection failed"); }
  };

  useEffect(() => { void load(); }, []);

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
        joiningDate:f.get("joiningDate"), isActive:true
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

  const nav = ["Dashboard","Organizations","Branches","Departments","Employees"];
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
      {tab==="Employees" && <Section title="Employees" form={createEmployee}><input name="code" placeholder="Employee code" required/><input name="name" placeholder="Full name" required/><Select name="branchId" placeholder="Branch (optional)" optional items={branches.map(x=>({id:x.id,label:x.branchName}))}/><Select name="departmentId" placeholder="Department (optional)" optional items={departments.map(x=>({id:x.id,label:x.departmentName}))}/><Select name="managerId" placeholder="Reporting manager (optional)" optional items={employees.map(x=>({id:x.id,label:x.employeeCode+" · "+x.fullName}))}/><input type="date" name="joiningDate" required/><button type="submit">Add Employee</button><div className="bulk-upload"><div><strong>Bulk Employee Upload</strong><span>CSV only · all rows are validated before import</span></div><input type="file" accept=".csv,text/csv" onChange={e=>setBulkFile(e.target.files?.[0] ?? null)}/><button type="button" onClick={downloadEmployeeTemplate}>Download Template</button><button type="button" onClick={()=>void uploadEmployees()}>Upload CSV</button>{bulkMessage && <div className="success">{bulkMessage}</div>}</div><div className="employee-table table-wrap"><table><thead><tr><th>Code</th><th>Name</th><th>Branch</th><th>Department</th><th>Status</th><th></th></tr></thead><tbody>{employees.length===0?<tr><td colSpan={6} className="empty">No records yet.</td></tr>:employees.map(x=><tr key={x.id}><td>{x.employeeCode}</td><td>{x.fullName}</td><td>{branches.find(b=>b.id===x.branchId)?.branchName??"-"}</td><td>{departments.find(d=>d.id===x.departmentId)?.departmentName??"-"}</td><td>{x.isActive?"Active":"Inactive"}</td><td><button type="button" className="link-button" onClick={()=>setSelectedEmployee(x)}>View / Edit</button></td></tr>)}</tbody></table></div></Section>}
      {selectedEmployee && <EmployeeDetails employee={selectedEmployee} branches={branches} departments={departments} employees={employees} onClose={()=>setSelectedEmployee(null)} onSubmit={updateEmployee}/>}

    </main>
  </div>;
}

function EmployeeDetails({employee,branches,departments,employees,onClose,onSubmit}:{employee:Employee;branches:Branch[];departments:Department[];employees:Employee[];onClose:()=>void;onSubmit:(e:FormEvent<HTMLFormElement>)=>void}) {
  return <div className="details-overlay"><div className="details-panel">
    <div className="details-head"><div><small>EMPLOYEE MASTER</small><h2>{employee.employeeCode} · {employee.fullName}</h2></div><button type="button" className="close-button" onClick={onClose}>Close</button></div>
    <form onSubmit={onSubmit} className="details-form">
      <h3>Personal Details</h3>
      <input name="employeeCode" defaultValue={employee.employeeCode} placeholder="Employee code" required/>
      <input name="fullName" defaultValue={employee.fullName} placeholder="Full name" required/>
      <input type="date" name="dateOfBirth" defaultValue={employee.dateOfBirth?.slice(0,10) ?? ""}/>
      <select name="gender" defaultValue={employee.gender ?? ""}><option value="">Gender</option><option>Male</option><option>Female</option><option>Other</option></select>
      <input name="mobileNumber" defaultValue={employee.mobileNumber ?? ""} placeholder="Mobile number"/>
      <input type="email" name="emailAddress" defaultValue={employee.emailAddress ?? ""} placeholder="Email address"/>
      <textarea name="address" defaultValue={employee.address ?? ""} placeholder="Address"/>
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
