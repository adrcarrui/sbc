import axios from 'axios';

export const sbcApi = axios.create({
  baseURL: '/api',
  timeout: 10000,
});