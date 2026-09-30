import { useEffect, useState } from "react";

const API = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

type Grade={id:string;gradeCode:string;gradeName:string;description?:string;isActive:boolean};
type Policy={id:string;policyCode:string;policyName:string;policyVersion:string;effectiveFrom:string;effectiveTo?:string;isActive:boolean};
type Rule={id:string;travelPolicyId:string;employeeGradeId?:string;ruleType:string;travelDuration:string;travelMode?:string;vehicleType?:string;amount?:number;ratePerKm?:number;maxKmPerDay?:number;calculationType?:string;requiresAttachment:boolean;notes?:string;isActive:boolean};

async function api<T>(path:string,options?:RequestInit):Promise<T>{
 const r=await fetch(API+path,{headers:{"Content-Type":"application/json",...(options?.headers??{})},...options});
 if(!r.ok) throw new Error((await r.text())||r.statusText);
 return r.json();
}

export default function ExpensePolicy(){
 const [grades,setGrades]=useState<Grade[]>([]);
 const [policies,setPolicies]=useState<Policy[]>([]);
 const [selected,setSelected]=useState<Policy|null>(null);
 const [rules,setRules]=useState<Rule[]>([]);
 const [error,setError]=useState("");
 const [message,setMessage]=useState("");
 const [gradeCode,setGradeCode]=useState(""); const [gradeName,setGradeName]=useState("");
 const [policyCode,setPolicyCode]=useState(""); const [policyName,setPolicyName]=useState(""); const [version,setVersion]=useState("1.10"); const [effective,setEffective]=useState("2025-08-01");

 const load=async()=>{try{setError("");const [g,p]=await Promise.all([api<Grade[]>("/api/employee-grades"),api<Policy[]>("/api/travel-policies")]);setGrades(g);setPolicies(p);if(!selected&&p[0]){setSelected(p[0]);await loadPolicy(p[0].id)}}catch(e){setError(e instanceof Error?e.message:"Could not load expense policy data")}};
 const loadPolicy=async(id:string)=>{try{const x=await api<{policy:Policy;rules:Rule[]}>(`/api/travel-policies/${id}`);setSelected(x.policy);setRules(x.rules)}catch(e){setError(e instanceof Error?e.message:"Could not load policy")}};
 useEffect(()=>{void load()},[]);

 const addGrade=async(e:React.FormEvent)=>{e.preventDefault();try{await api("/api/employee-grades",{method:"POST",body:JSON.stringify({gradeCode,gradeName,isActive:true})});setGradeCode("");setGradeName("");setMessage("Grade added.");await load()}catch(e){setError(e instanceof Error?e.message:"Could not add grade")}};
 const addPolicy=async(e:React.FormEvent)=>{e.preventDefault();try{const p=await api<Policy>("/api/travel-policies",{method:"POST",body:JSON.stringify({policyCode,policyName,policyVersion:version,effectiveFrom:effective,isActive:true})});setPolicyCode("");setPolicyName("");setMessage("Travel policy added.");await loadPolicy(p.id);await load()}catch(e){setError(e instanceof Error?e.message:"Could not add policy")}};
 const seedGradeRule=async(e:React.FormEvent)=>{e.preventDefault();if(!selected)return;const f=new FormData(e.currentTarget);try{await api(`/api/travel-policies/${selected.id}/rules`,{method:"POST",body:JSON.stringify({employeeGradeId:f.get("gradeId")||null,ruleType:f.get("ruleType"),travelDuration:f.get("duration"),travelMode:f.get("travelMode")||null,vehicleType:f.get("vehicleType")||null,amount:f.get("amount")?Number(f.get("amount")):null,ratePerKm:f.get("rate")?Number(f.get("rate")):null,maxKmPerDay:f.get("maxKm")?Number(f.get("maxKm")):null,calculationType:f.get("calculation")||null,requiresAttachment:f.get("attachment")==="on",notes:f.get("notes")||null,isActive:true})});e.currentTarget.reset();setMessage("Rule added.");await loadPolicy(selected.id)}catch(e){setError(e instanceof Error?e.message:"Could not add rule")}};
 return <div className="content">
  {error&&<div className="error">{error}</div>}{message&&<div className="success">{message}</div>}
  <div className="panel"><h2>Employee Grades</h2><p className="muted">Travel policy rules are bound to the employee grade assigned to each employee.</p><form onSubmit={addGrade}><input value={gradeCode} onChange={e=>setGradeCode(e.target.value)} placeholder="Grade code e.g. P3" required/><input value={gradeName} onChange={e=>setGradeName(e.target.value)} placeholder="Grade name" required/><button>Add Grade</button></form><div className="table-wrap"><table><thead><tr><th>Code</th><th>Name</th><th>Status</th></tr></thead><tbody>{grades.map(g=><tr key={g.id}><td>{g.gradeCode}</td><td>{g.gradeName}</td><td>{g.isActive?"Active":"Inactive"}</td></tr>)}</tbody></table></div></div>
  <div className="panel"><h2>Travel Policies</h2><form onSubmit={addPolicy}><input value={policyCode} onChange={e=>setPolicyCode(e.target.value)} placeholder="Policy code" required/><input value={policyName} onChange={e=>setPolicyName(e.target.value)} placeholder="Policy name" required/><input value={version} onChange={e=>setVersion(e.target.value)} placeholder="Version" required/><input type="date" value={effective} onChange={e=>setEffective(e.target.value)} required/><button>Add Policy</button></form><div className="table-wrap"><table><thead><tr><th>Code</th><th>Name</th><th>Version</th><th>Effective</th><th></th></tr></thead><tbody>{policies.map(p=><tr key={p.id}><td>{p.policyCode}</td><td>{p.policyName}</td><td>{p.policyVersion}</td><td>{p.effectiveFrom}</td><td><button type="button" className="link-button" onClick={()=>void loadPolicy(p.id)}>Manage Rules</button></td></tr>)}</tbody></table></div></div>
  {selected&&<div className="panel"><h2>Rules · {selected.policyCode} v{selected.policyVersion}</h2><p className="muted">Use the source policy values here. Marketing/employee exceptions should be added as exception rules; petrol reimbursement is not calculated unless a policy rule explicitly supplies the rate/formula.</p><form onSubmit={seedGradeRule}>
   <select name="gradeId" required><option value="">Employee grade</option>{grades.map(g=><option key={g.id} value={g.id}>{g.gradeCode} · {g.gradeName}</option>)}</select>
   <input name="ruleType" placeholder="Rule type: Lodging / Food / TravelMode / Vehicle / Attachment" required/>
   <select name="duration"><option>All</option><option>SingleDay</option><option>MultiDay</option></select>
   <input name="travelMode" placeholder="Travel mode e.g. Train / Air / Bus / Local"/>
   <input name="vehicleType" placeholder="Vehicle e.g. Bike / Car"/>
   <input name="amount" type="number" step="0.01" placeholder="Amount"/>
   <input name="rate" type="number" step="0.01" placeholder="Rate per km"/>
   <input name="maxKm" type="number" step="0.01" placeholder="Max km/day"/>
   <input name="calculation" placeholder="Calculation type"/>
   <label className="check-field"><input name="attachment" type="checkbox"/> Attachment required</label>
   <input name="notes" placeholder="Notes / source wording"/>
   <button>Add Rule</button>
  </form><div className="table-wrap"><table><thead><tr><th>Grade</th><th>Rule</th><th>Duration</th><th>Mode</th><th>Vehicle</th><th>Amount</th><th>Rate/km</th><th>Max km/day</th><th>Attachment</th></tr></thead><tbody>{rules.map(r=><tr key={r.id}><td>{grades.find(g=>g.id===r.employeeGradeId)?.gradeCode??"All"}</td><td>{r.ruleType}</td><td>{r.travelDuration}</td><td>{r.travelMode??"-"}</td><td>{r.vehicleType??"-"}</td><td>{r.amount??"-"}</td><td>{r.ratePerKm??"-"}</td><td>{r.maxKmPerDay??"-"}</td><td>{r.requiresAttachment?"Yes":"No"}</td></tr>)}</tbody></table></div></div>}
 </div>;
}
