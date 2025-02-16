
const BASE_URL = process.env.NEXT_PUBLIC_BASE_URL ? process.env.NEXT_PUBLIC_BASE_URL: "http://localhost:8080";

const BACKEND_URL = `${BASE_URL}/api`;
const SIGNALR_URL = `${BASE_URL}/sessionHub`;
export { BACKEND_URL , SIGNALR_URL };
