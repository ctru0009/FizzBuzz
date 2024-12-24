'use client';
import isAuth from '@/components/IsAuth';
import React, { useLayoutEffect } from 'react'

const page = () => {
  return (
    <div>index</div>
  )
}

export default isAuth(page)