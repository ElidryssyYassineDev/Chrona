import './App.css'
import { useAuth } from 'react-oidc-context'
import { useState } from 'react';
import { useEffect } from 'react';

interface Employee {
  id: string
  firstName: string
  lastName: string
  isActive: boolean
}

function App() {
  const auth                    = useAuth();
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [status, setStatus]     = useState<'idle' | 'loading' | 'success' | 'unauthorized' | 'notFound' | 'error'>('idle');
  const [error, setError]       = useState<string | null>(null);
  console.log(auth.user);

  useEffect(()=>{
    if(!auth.isAuthenticated){
      return;
    }

    const fetchEmployee = async ()=>{
      setStatus('loading');
      const accessToken = 'garbage';
      try {
        const response = await fetch(
          'http://localhost:5294/api/v1/employees/me',
          {
            headers:{
            Authorization: `Bearer ${accessToken}`,
            },
          }
        )
        //1st edge case: 401 UNAUTHORIZED
        if(response.status === 401){
          setStatus("unauthorized")
          return
        }
        //2nd edge case: 404 Not found
        if (response.status === 404){
          setStatus("notFound")
          return
        }
        //3rd edge case: generic error
        if (!response.ok){
          setStatus("error")
          setError(`Request failed with status ${response.status}`)
          return
        }
        //happy path
        const data: Employee = await response.json();
        setEmployee(data)
        setStatus('success')

      } catch (err) {
        setStatus("error")
        setError('failed to fetch!')
      }
      


    }
    fetchEmployee();
  },[auth.isAuthenticated]);

  return (
    <>
      <section id="center">
        {auth.isLoading ? (
        <div>Loading...</div>
      ) : !auth.isAuthenticated ? (
        <button onClick={() => auth.signinRedirect()}>
          Login
        </button>
      ) : status === 'loading' ? (
        <div>Loading employee...</div>
      ): status === 'unauthorized'? (
        <div>session expired</div>
      ): status === 'notFound' ? (
        <div>No employee record</div>
      ): status === 'error' ? (
        <div>{error}</div>
      ) : employee ? (
        <div>
          <h1>{employee.firstName} {employee.lastName}</h1>
          <p>ID: {employee.id}</p>
          <p>Active: {employee.isActive ? 'Yes' : 'No'}</p>
        </div>
      ) : null}
      </section>
    </>
  )
}

export default App
