import { FormEvent, ReactNode, useEffect, useState } from "react";

const API = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

type Organization = { id: string; organizationCode: string; organizationName: string; legalName?: string; timeZoneId?: string; isActive: boolean };
type Branch = { id: string; organizationId: string; branchCode: string; branchName: string; address?: string; isActive: boolean };
type Department = { id: string; organizationId: string; departmentCode: string; departmentName: string; isActive: boolean };
type Employee = { id: string; employeeCode: string; fullName: string; departmentId?: string; branchId?: string; reportingManagerId?: string; joiningDate: string; isActive: boolean };

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
    const f = new FormData(event.currentTarget);
    try {
      await api("/api/organizations", { method: "POST", body: JSON.stringify({
        organizationCode: f.get("code"), organizationName: f.get("name"), legalName: f.get("legalName") || null,
        timeZoneId: "Asia/Kolkata", isActive: true
      })});
      event.currentTarget.reset(); await load();
    } catch(e) { setError(e instanceof Error ? e.message : "Could not create organization"); }
  };

  const createBranch = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const f = new FormData(event.currentTarget);
    try {
      await api("/api/branches", { method:"POST", body: JSON.stringify({
        organizationId:f.get("organizationId"), branchCode:f.get("code"), branchName:f.get("name"), address:f.get("address") || null, isActive:true
      })}); event.currentTarget.reset(); await load();
    } catch(e) { setError(e instanceof Error ? e.message : "Could not create branch"); }
  };

  const createDepartment = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const f = new FormData(event.currentTarget);
    try {
      await api("/api/departments", { method:"POST", body: JSON.stringify({
        organizationId:f.get("organizationId"), departmentCode:f.get("code"), departmentName:f.get("name"), isActive:true
      })}); event.currentTarget.reset(); await load();
    } catch(e) { setError(e instanceof Error ? e.message : "Could not create department"); }
  };

  const createEmployee = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const f = new FormData(event.currentTarget);
    try {
      await api("/api/employees", { method:"POST", body: JSON.stringify({
        employeeCode:f.get("code"), fullName:f.get("name"), branchId:f.get("branchId") || null,
        departmentId:f.get("departmentId") || null, reportingManagerId:f.get("managerId") || null,
        joiningDate:f.get("joiningDate"), isActive:true
      })}); event.currentTarget.reset(); await load();
    } catch(e) { setError(e instanceof Error ? e.message : "Could not create employee"); }
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
      {tab==="Employees" && <Section title="Employees" form={createEmployee}><input name="code" placeholder="Employee code" required/><input name="name" placeholder="Full name" required/><Select name="branchId" placeholder="Branch (optional)" optional items={branches.map(x=>({id:x.id,label:x.branchName}))}/><Select name="departmentId" placeholder="Department (optional)" optional items={departments.map(x=>({id:x.id,label:x.departmentName}))}/><Select name="managerId" placeholder="Reporting manager (optional)" optional items={employees.map(x=>({id:x.id,label:x.employeeCode+" · "+x.fullName}))}/><input type="date" name="joiningDate" required/><button type="submit">Add Employee</button><List rows={employees.map(x=>[x.employeeCode,x.fullName,branches.find(b=>b.id===x.branchId)?.branchName??"-",departments.find(d=>d.id===x.departmentId)?.departmentName??"-",x.isActive?"Active":"Inactive"])}/></Section>}
    </main>
  </div>;
}

function Dashboard({organizations,branches,departments,employees}:{organizations:Organization[];branches:Branch[];departments:Department[];employees:Employee[]}) {
  return <><div className="welcome"><div><small>WELCOME</small><h2>HRMS foundation is ready.</h2><p>Set up your organization structure and employee master before connecting biometric attendance.</p></div></div><div className="cards">{[["Organizations",organizations.length],["Branches",branches.length],["Departments",departments.length],["Active Employees",employees.length]].map(([a,b])=><div className="card" key={String(a)}><span>{a}</span><strong>{b}</strong></div>)}</div><div className="panel"><h3>Implementation path</h3><div className="steps"><span>01 Master Data</span><span>02 Users & Roles</span><span>03 Devices & Punches</span><span>04 Attendance Engine</span><span>05 Leave Workflow</span></div></div></>;
}
function Section({title,form,children}:{title:string;form:(e:FormEvent<HTMLFormElement>)=>void;children:ReactNode}) {
  return <div className="content"><div className="panel"><h2>Add {title.slice(0,-1)}</h2><form onSubmit={form}>{children}</form></div></div>;
}
function Select({name,placeholder,items,optional}:{name:string;placeholder:string;items:{id:string;label:string}[];optional?:boolean}) {
  return <select name={name} required={!optional} defaultValue=""><option value="">{placeholder}</option>{items.map(x=><option value={x.id} key={x.id}>{x.label}</option>)}</select>;
}
function List({rows}:{rows:string[][]}) {
  return <div className="table-wrap"><table><tbody>{rows.length===0?<tr><td className="empty">No records yet.</td></tr>:rows.map((r,i)=><tr key={i}>{r.map((c,j)=><td key={j}>{c}</td>)}</tr>)}</tbody></table></div>;
}
export default App;
